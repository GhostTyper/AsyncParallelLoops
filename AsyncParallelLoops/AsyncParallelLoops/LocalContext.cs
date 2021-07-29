using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharpFast.Async
{
    class LocalContext<TSource, THolder> where THolder : class
    {
        private THolder? holder;
        private object sync;
        private TaskCompletionSource tcs;
        private IEnumerator<TSource> enumerator;
        private GlobalState state;

        private HolderHandler<TSource, THolder> body;
        private InitHandler<THolder>? init;
        private FinalizeHandler<THolder>? finalize;
        private ExceptionHandler<TSource, THolder>? exception;

        public LocalContext(object sync, IEnumerator<TSource> enumerator, GlobalState state, HolderHandler<TSource, THolder> body, InitHandler<THolder>? init, FinalizeHandler<THolder>? finalize, ExceptionHandler<TSource, THolder>? exception)
        {
            this.state = state;
            this.sync = sync;
            this.enumerator = enumerator;
            tcs = new TaskCompletionSource();

            this.body = body;
            this.init = init;
            this.finalize = finalize;
            this.exception = exception;
        }

        public void Execute()
        {
            ThreadPool.QueueUserWorkItem(async delegate { await execute(); });
        }

        private async Task execute()
        {
            TSource source;

            if (init != null)
                try
                {
                    holder = await init();
                }
                catch (Exception exception)
                {
                    tcs.SetException(new InvalidOperationException("Init method failed.", exception));
                    return;
                }

            while (true)
            {
                lock (sync)
                {
                    if (!state.Running)
                        break;

                    if (!enumerator.MoveNext())
                    {
                        state.Running = false;
                        break;
                    }

                    source = enumerator.Current;
                }

                try
                {
                    await body(source, holder);
                }
                catch (Exception exception)
                {
                    if (this.exception != null)
                        try
                        {
                            await this.exception(source, holder, exception);
                        }
                        catch (Exception subException)
                        {
                            tcs.SetException(new InvalidOperationException("Exception handler failed while handling an exception.", subException));
                            break;
                        }
                    else
                    {
                        tcs.SetException(new InvalidOperationException("Exception within regular call but no exception handler bound.", exception));
                        break;
                    }
                }
            }

            if (finalize != null)
                try
                {
                    await finalize(holder);
                }
                catch (Exception exception)
                {
                    tcs.TrySetException(new InvalidOperationException("Finalize method failed.", exception));
                }

            tcs.TrySetResult();
        }

        public async Task Wait()
        {
            await tcs.Task;
        }
    }
}
