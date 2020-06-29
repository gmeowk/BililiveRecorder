using System;
using System.Threading;
using System.Threading.Tasks;

namespace BililiveRecorder
{
    public interface IConvertMediaTaskQueue
    {
        void ConvertMediaTaskItem(Func<CancellationToken, Task> workItem);

        Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
    }
}
