using System;
using System.Threading.Tasks;

namespace TtsApi.Helper
{
    public class BasicBucket
    {
        private readonly int _limit;
        private readonly int _perXSeconds;
        private int _usedTickets;
        private const float LimitBuffer = 0.9f;
        private const float TimeBuffer = 1.1f;

        /// <summary>
        /// Creates a basic bucket with.
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="perXSeconds"></param>
        public BasicBucket(int limit, int perXSeconds = 30)
        {
            _limit = (int)(limit * LimitBuffer);
            _perXSeconds = (int)(perXSeconds * TimeBuffer);
        }

        /// <summary>
        /// Amount of tickets remaining in the bucket.
        /// </summary>
        public int TicketsRemaining => _limit - _usedTickets;

        /// <summary>
        /// Is at least one ticket remaining in the bucket.
        /// </summary>
        public bool TicketAvailable => TicketsRemaining > 0;

        /// <summary>
        /// Take X amount of tickets out o the bucket if tickets are available. <br/>
        /// If no tickets available indicate via the return value. <br />
        /// To await until a ticket is available use:
        /// <code>
        /// while (!bucket.TakeTicket())
        ///     await Task.Delay(100);
        /// </code>
        /// </summary>
        /// <param name="amount">Amount of tickets to take.</param>
        /// <returns>Was the ticket take successfully.</returns>
        public bool TakeTicket(int amount = 1)
        {
            if (_usedTickets + amount > _limit)
                return false;

            _usedTickets += amount;
            Task.Delay(new TimeSpan(0, 0, _perXSeconds)).ContinueWith(ReturnTicket);
            return true;
        }

        /// <summary>
        /// Return a ticket. This function should only ever called by the <see cref="Task.Delay(int)"/>
        /// started in <see cref="TakeTicket"/> running out.
        /// </summary>
        /// <param name="task"><see cref="Task.Delay(int)"/> task.
        /// Used in <see cref="Task.ContinueWith(System.Action{System.Threading.Tasks.Task})"/></param>
        private void ReturnTicket(Task task)
        {
            if (_usedTickets > 0)
                _usedTickets--;
        }
    }
}
