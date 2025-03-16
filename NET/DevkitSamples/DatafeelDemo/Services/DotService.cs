using Datafeel;
using Datafeel.NET.Serial;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatafeelDemo
{
    public sealed class DotService : IDotService
    {
        public DotManager DotManager { get; init; } = new();

        public ILogger Logger { get; init; }

        public DotService(ILogger logger)
        {
            Logger = logger;
        }

        public DotService()
        {
            DotManager = new DotManagerConfiguration()
                .AddDot<Dot_63x_xxx>(1)
                .AddDot<Dot_63x_xxx>(2)
                .AddDot<Dot_63x_xxx>(3)
                .AddDot<Dot_63x_xxx>(4)
                .CreateDotManager();
        }

        //public async Task Start(int numDots, CancellationTokenSource cts)
        //{
        //    DotManager = new DotManager();

        //    await Task.Run(async () =>
        //    {
        //        while ()
        //        {

        //        }
        //        await DotManager.Connect(1);
        //    });
        //}
    }
}
