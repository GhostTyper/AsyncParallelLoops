using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharpFast.Async
{
    public delegate Task HolderHandler<TSource, THolder>(TSource source, THolder? holder) where THolder : class;
    public delegate Task<THolder> InitHandler<THolder>() where THolder : class;
    public delegate Task ExceptionHandler<TSource, THolder>(TSource source, THolder? holder, Exception exception) where THolder : class;
    public delegate Task FinalizeHandler<THolder>(THolder? holder) where THolder : class;

    public static class ParallelLoops
    {
        public static async Task ForEach<TSource, THolder>(IEnumerable<TSource> source, HolderHandler<TSource, THolder> body, int threads = 4, InitHandler<THolder>? init = null, FinalizeHandler<THolder>? finalize = null, ExceptionHandler<TSource, THolder>? exception = null) where THolder : class
        {
            LocalContext<TSource, THolder>[] contexts = new LocalContext<TSource, THolder>[threads];
            object sync = new object();
            GlobalState state = new GlobalState();

            IEnumerator<TSource> enumerator = source.GetEnumerator();

            for (int position = 0; position < contexts.Length; position++)
                contexts[position] = new LocalContext<TSource, THolder>(sync, enumerator, state, body, init, finalize, exception);

            foreach (LocalContext<TSource, THolder> context in contexts)
                context.Execute();

            Exception? toThrow = null;

            foreach (LocalContext<TSource, THolder> context in contexts)
                try
                {
                    await context.Wait();
                }
                catch (Exception innerException)
                {
                    toThrow = innerException;
                }

            if (toThrow != null)
                throw toThrow;
        }
    }
}
