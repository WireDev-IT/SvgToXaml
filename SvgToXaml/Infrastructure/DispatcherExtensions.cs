using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SvgToXaml.Infrastructure
{
    public static class DispatcherExtensions
    {
        /// <summary>
        /// Führt die Action über den UI-Dispatcher aus.
        /// </summary>
        /// <param name="action">Auszuführende Aktion</param>
        public static void InUi(Action action)
        {
            if (Application.Current == null)
            {
                action();
                return;
            }

            Application.Current.Dispatcher.Do(action);
        }

        public static Task InUiAsync(Action action)
        {
            return Application.Current == null ? RunSynchronously(action) : Application.Current.Dispatcher.DoAsync(action);
        }

        private static void Do(this Dispatcher dispatcher, Action action)
        {
            if (!dispatcher.CheckAccess())
            {
                _ = dispatcher.BeginInvoke(action, DispatcherPriority.Background);
                return;
            }

            action();
        }

        private static Task DoAsync(this Dispatcher dispatcher, Action action)
        {
            return !dispatcher.CheckAccess() ? RunAsync(dispatcher, action) : RunSynchronously(action);
        }

        private static Task RunAsync(Dispatcher dispatcher, Action action)
        {
            TaskCompletionSource<object> completionSource = new TaskCompletionSource<object>();
            DispatcherOperation dispatcherOperation = dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    completionSource.SetException(ex);
                }
            }), DispatcherPriority.Background);
            dispatcherOperation.Aborted += (s, e) => completionSource.SetCanceled();
            dispatcherOperation.Completed += (s, e) => completionSource.SetResult(null);
            return completionSource.Task;
        }

        private static Task RunSynchronously(Action action)
        {
            TaskCompletionSource<object> completionSource = new TaskCompletionSource<object>();
            try
            {
                action();
                completionSource.SetResult(null);
            }
            catch (Exception ex)
            {
                completionSource.SetException(ex);
            }
            return completionSource.Task;
        }
    }

}
