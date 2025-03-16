using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datafeel;

namespace DatafeelDemo
{
    public interface IDotService
    {
        DotManager DotManager { get; }
        //Task Start(int numDots);
    }
}
