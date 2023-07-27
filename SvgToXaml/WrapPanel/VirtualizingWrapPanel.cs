using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SvgToXaml.WrapPanel
{
    public class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo
    {
        private const double ScrollLineAmount = 16.0;

        private Size _extentSize;
        private Size _viewportSize;
        private Point _offset;
        private ItemsControl _itemsControl;
        private readonly Dictionary<UIElement, Rect> _childLayouts = new Dictionary<UIElement, Rect>();

        public static readonly DependencyProperty ItemWidthProperty =
            DependencyProperty.Register("ItemWidth", typeof(double), typeof(VirtualizingWrapPanel), new PropertyMetadata(32.0, HandleItemDimensionChanged));

        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register("ItemHeight", typeof(double), typeof(VirtualizingWrapPanel), new PropertyMetadata(32.0, HandleItemDimensionChanged));

        private static readonly DependencyProperty VirtualItemIndexProperty =
            DependencyProperty.RegisterAttached("VirtualItemIndex", typeof(int), typeof(VirtualizingWrapPanel), new PropertyMetadata(-1));
        private IRecyclingItemContainerGenerator _itemsGenerator;

        private bool _isInMeasure;

        private static int GetVirtualItemIndex(DependencyObject obj)
        {
            return (int)obj.GetValue(VirtualItemIndexProperty);
        }

        private static void SetVirtualItemIndex(DependencyObject obj, int value)
        {
            obj.SetValue(VirtualItemIndexProperty, value);
        }

        public double ItemHeight
        {
            get => (double)GetValue(ItemHeightProperty);
            set => SetValue(ItemHeightProperty, value);
        }

        public double ItemWidth
        {
            get => (double)GetValue(ItemWidthProperty);
            set => SetValue(ItemWidthProperty, value);
        }

        public VirtualizingWrapPanel()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _ = Dispatcher.BeginInvoke((Action)Initialize);
            }
        }

        private void Initialize()
        {
            _itemsControl = ItemsControl.GetItemsOwner(this);
            _itemsGenerator = (IRecyclingItemContainerGenerator)ItemContainerGenerator;

            InvalidateMeasure();
        }

        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            base.OnItemsChanged(sender, args);

            InvalidateMeasure();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (_itemsControl == null)
            {
                return availableSize;
            }

            _isInMeasure = true;
            _childLayouts.Clear();

            ExtentInfo extentInfo = GetExtentInfo(availableSize);

            EnsureScrollOffsetIsWithinConstrains(extentInfo);

            ItemLayoutInfo layoutInfo = GetLayoutInfo(availableSize, ItemHeight, extentInfo);

            RecycleItems(layoutInfo);

            // Determine where the first item is in relation to previously realized items
            GeneratorPosition generatorStartPosition = _itemsGenerator.GeneratorPositionFromIndex(layoutInfo.FirstRealizedItemIndex);

            int visualIndex = 0;

            double currentX = layoutInfo.FirstRealizedItemLeft;
            double currentY = layoutInfo.FirstRealizedLineTop;

            using (_itemsGenerator.StartAt(generatorStartPosition, GeneratorDirection.Forward, true))
            {
                for (int itemIndex = layoutInfo.FirstRealizedItemIndex; itemIndex <= layoutInfo.LastRealizedItemIndex; itemIndex++, visualIndex++)
                {

                    UIElement child = (UIElement)_itemsGenerator.GenerateNext(out bool newlyRealized);
                    SetVirtualItemIndex(child, itemIndex);

                    if (newlyRealized)
                    {
                        InsertInternalChild(visualIndex, child);
                    }
                    else
                    {
                        // check if item needs to be moved into a new position in the Children collection
                        if (visualIndex < Children.Count)
                        {
                            if (!ReferenceEquals(Children[visualIndex], child))
                            {
                                int childCurrentIndex = Children.IndexOf(child);

                                if (childCurrentIndex >= 0)
                                {
                                    RemoveInternalChildRange(childCurrentIndex, 1);
                                }

                                InsertInternalChild(visualIndex, child);
                            }
                        }
                        else
                        {
                            // we know that the child can't already be in the children collection
                            // because we've been inserting children in correct visualIndex order,
                            // and this child has a visualIndex greater than the Children.Count
                            AddInternalChild(child);
                        }
                    }

                    // only prepare the item once it has been added to the visual tree
                    _itemsGenerator.PrepareItemContainer(child);

                    child.Measure(new Size(ItemWidth, ItemHeight));

                    _childLayouts.Add(child, new Rect(currentX, currentY, ItemWidth, ItemHeight));

                    if (currentX + (ItemWidth * 2) >= availableSize.Width)
                    {
                        // wrap to a new line
                        currentY += ItemHeight;
                        currentX = 0;
                    }
                    else
                    {
                        currentX += ItemWidth;
                    }
                }
            }

            RemoveRedundantChildren();
            UpdateScrollInfo(availableSize, extentInfo);

            Size desiredSize = new Size(double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width,
                                       double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height);

            _isInMeasure = false;

            return desiredSize;
        }

        private void EnsureScrollOffsetIsWithinConstrains(ExtentInfo extentInfo)
        {
            _offset.Y = Clamp(_offset.Y, 0, extentInfo.MaxVerticalOffset);
        }

        private void RecycleItems(ItemLayoutInfo layoutInfo)
        {
            foreach (UIElement child in Children)
            {
                int virtualItemIndex = GetVirtualItemIndex(child);

                if (virtualItemIndex < layoutInfo.FirstRealizedItemIndex || virtualItemIndex > layoutInfo.LastRealizedItemIndex)
                {
                    GeneratorPosition generatorPosition = _itemsGenerator.GeneratorPositionFromIndex(virtualItemIndex);
                    if (generatorPosition.Index >= 0)
                    {
                        _itemsGenerator.Recycle(generatorPosition, 1);
                    }
                }

                SetVirtualItemIndex(child, -1);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (UIElement child in Children)
            {
                child.Arrange(_childLayouts[child]);
            }

            return finalSize;
        }

        private void UpdateScrollInfo(Size availableSize, ExtentInfo extentInfo)
        {
            _viewportSize = availableSize;
            _extentSize = new Size(availableSize.Width, extentInfo.ExtentHeight);

            InvalidateScrollInfo();
        }

        private void RemoveRedundantChildren()
        {
            // iterate backwards through the child collection because we're going to be
            // removing items from it
            for (int i = Children.Count - 1; i >= 0; i--)
            {
                UIElement child = Children[i];

                // if the virtual item index is -1, this indicates
                // it is a recycled item that hasn't been reused this time round
                if (GetVirtualItemIndex(child) == -1)
                {
                    RemoveInternalChildRange(i, 1);
                }
            }
        }

        private ItemLayoutInfo GetLayoutInfo(Size availableSize, double itemHeight, ExtentInfo extentInfo)
        {
            if (_itemsControl == null)
            {
                return new ItemLayoutInfo();
            }

            // we need to ensure that there is one realized item prior to the first visible item, and one after the last visible item,
            // so that keyboard navigation works properly. For example, when focus is on the first visible item, and the user
            // navigates up, the ListBox selects the previous item, and the scrolls that into view - and this triggers the loading of the rest of the items 
            // in that row

            int firstVisibleLine = (int)Math.Floor(VerticalOffset / itemHeight);

            int firstRealizedIndex = Math.Max((extentInfo.ItemsPerLine * firstVisibleLine) - 1, 0);
            double firstRealizedItemLeft = (firstRealizedIndex % extentInfo.ItemsPerLine * ItemWidth) - HorizontalOffset;
            // ReSharper disable once PossibleLossOfFraction
            double firstRealizedItemTop = (firstRealizedIndex / extentInfo.ItemsPerLine * itemHeight) - VerticalOffset;

            double firstCompleteLineTop = firstVisibleLine == 0 ? firstRealizedItemTop : firstRealizedItemTop + ItemHeight;
            int completeRealizedLines = (int)Math.Ceiling((availableSize.Height - firstCompleteLineTop) / itemHeight);

            int lastRealizedIndex = Math.Min(firstRealizedIndex + (completeRealizedLines * extentInfo.ItemsPerLine) + 2, _itemsControl.Items.Count - 1);

            return new ItemLayoutInfo
            {
                FirstRealizedItemIndex = firstRealizedIndex,
                FirstRealizedItemLeft = firstRealizedItemLeft,
                FirstRealizedLineTop = firstRealizedItemTop,
                LastRealizedItemIndex = lastRealizedIndex,
            };
        }

        private ExtentInfo GetExtentInfo(Size viewPortSize)
        {
            if (_itemsControl == null)
            {
                return new ExtentInfo();
            }

            int itemsPerLine = Math.Max((int)Math.Floor(viewPortSize.Width / ItemWidth), 1);
            int totalLines = (int)Math.Ceiling((double)_itemsControl.Items.Count / itemsPerLine);
            double extentHeight = Math.Max(totalLines * ItemHeight, viewPortSize.Height);

            return new ExtentInfo
            {
                ItemsPerLine = itemsPerLine,
                TotalLines = totalLines,
                ExtentHeight = extentHeight,
                MaxVerticalOffset = extentHeight - viewPortSize.Height,
            };
        }

        public void LineUp()
        {
            SetVerticalOffset(VerticalOffset - ScrollLineAmount);
        }

        public void LineDown()
        {
            SetVerticalOffset(VerticalOffset + ScrollLineAmount);
        }

        public void LineLeft()
        {
            SetHorizontalOffset(HorizontalOffset + ScrollLineAmount);
        }

        public void LineRight()
        {
            SetHorizontalOffset(HorizontalOffset - ScrollLineAmount);
        }

        public void PageUp()
        {
            SetVerticalOffset(VerticalOffset - ViewportHeight);
        }

        public void PageDown()
        {
            SetVerticalOffset(VerticalOffset + ViewportHeight);
        }

        public void PageLeft()
        {
            SetHorizontalOffset(HorizontalOffset + ItemWidth);
        }

        public void PageRight()
        {
            SetHorizontalOffset(HorizontalOffset - ItemWidth);
        }

        public void MouseWheelUp()
        {
            SetVerticalOffset(VerticalOffset - (ScrollLineAmount * SystemParameters.WheelScrollLines));
        }

        public void MouseWheelDown()
        {
            SetVerticalOffset(VerticalOffset + (ScrollLineAmount * SystemParameters.WheelScrollLines));
        }

        public void MouseWheelLeft()
        {
            SetHorizontalOffset(HorizontalOffset - (ScrollLineAmount * SystemParameters.WheelScrollLines));
        }

        public void MouseWheelRight()
        {
            SetHorizontalOffset(HorizontalOffset + (ScrollLineAmount * SystemParameters.WheelScrollLines));
        }

        public void SetHorizontalOffset(double offset)
        {
            if (_isInMeasure)
            {
                return;
            }

            offset = Clamp(offset, 0, ExtentWidth - ViewportWidth);
            _offset = new Point(offset, _offset.Y);

            InvalidateScrollInfo();
            InvalidateMeasure();
        }

        public void SetVerticalOffset(double offset)
        {
            if (_isInMeasure)
            {
                return;
            }

            offset = Clamp(offset, 0, ExtentHeight - ViewportHeight);
            _offset = new Point(_offset.X, offset);

            InvalidateScrollInfo();
            InvalidateMeasure();
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            if (rectangle.IsEmpty ||
                visual == null ||
                ReferenceEquals(visual, this) ||
                !IsAncestorOf(visual))
            {
                return Rect.Empty;
            }

            rectangle = visual.TransformToAncestor(this).TransformBounds(rectangle);

            Rect viewRect = new Rect(HorizontalOffset, VerticalOffset, ViewportWidth, ViewportHeight);
            rectangle.X += viewRect.X;
            rectangle.Y += viewRect.Y;

            viewRect.X = CalculateNewScrollOffset(viewRect.Left, viewRect.Right, rectangle.Left, rectangle.Right);
            viewRect.Y = CalculateNewScrollOffset(viewRect.Top, viewRect.Bottom, rectangle.Top, rectangle.Bottom);

            SetHorizontalOffset(viewRect.X);
            SetVerticalOffset(viewRect.Y);
            rectangle.Intersect(viewRect);

            rectangle.X -= viewRect.X;
            rectangle.Y -= viewRect.Y;

            return rectangle;
        }

        private static double CalculateNewScrollOffset(double topView, double bottomView, double topChild, double bottomChild)
        {
            bool offBottom = topChild < topView && bottomChild < bottomView;
            bool offTop = bottomChild > bottomView && topChild > topView;
            bool tooLarge = (bottomChild - topChild) > (bottomView - topView);

            return !offBottom && !offTop
                ? topView
                : (offBottom && !tooLarge) || (offTop && tooLarge) ? topChild : bottomChild - (bottomView - topView);
        }


        public ItemLayoutInfo GetVisibleItemsRange()
        {
            return GetLayoutInfo(_viewportSize, ItemHeight, GetExtentInfo(_viewportSize));
        }

        public bool CanVerticallyScroll
        {
            get;
            set;
        }

        public bool CanHorizontallyScroll
        {
            get;
            set;
        }

        public double ExtentWidth => _extentSize.Width;

        public double ExtentHeight => _extentSize.Height;

        public double ViewportWidth => _viewportSize.Width;

        public double ViewportHeight => _viewportSize.Height;

        public double HorizontalOffset => _offset.X;

        public double VerticalOffset => _offset.Y;

        public ScrollViewer ScrollOwner
        {
            get;
            set;
        }

        private void InvalidateScrollInfo()
        {
            ScrollOwner?.InvalidateScrollInfo();
        }

        private static void HandleItemDimensionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            VirtualizingWrapPanel wrapPanel = d as VirtualizingWrapPanel;

            wrapPanel?.InvalidateMeasure();
        }

        private double Clamp(double value, double min, double max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        internal class ExtentInfo
        {
            public int ItemsPerLine;
            public int TotalLines;
            public double ExtentHeight;
            public double MaxVerticalOffset;
        }

        public class ItemLayoutInfo
        {
            public int FirstRealizedItemIndex;
            public double FirstRealizedLineTop;
            public double FirstRealizedItemLeft;
            public int LastRealizedItemIndex;
        }
    }
}
