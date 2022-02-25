using System;
using System.Threading.Tasks;

namespace BlazorComponent;

public interface IDelayable
{
    int OpenDelay { get; set; }

    int CloseDelay { get; set; }

    Task RunOpenDelayAsync(Func<Task> cb = null);

    Task RunCloseDelayAsync(Func<Task> cb = null);
}