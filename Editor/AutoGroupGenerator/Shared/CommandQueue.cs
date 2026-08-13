using System;
using System.Collections.Generic;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Queue wrapper that executes commands in sequence.
    /// </summary>
    public class CommandQueue
    {
        #region Fields
        private readonly Queue<Command> m_ProcessingQueue = new Queue<Command>();
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets a title describing the queue.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets the number of commands remaining in the queue.
        /// </summary>
        public int RemainingCommandCount => m_ProcessingQueue.Count;
        #endregion

        #region Methods
        /// <summary>
        /// Initializes an empty command queue.
        /// </summary>
        public CommandQueue()
        {
        }

        /// <summary>
        /// Initializes a queue with a single command.
        /// </summary>
        /// <param name="action">Action to execute.</param>
        /// <param name="info">Description of the action.</param>
        public CommandQueue(Action action, string info)
        {
            AddCommand(action, info);

            Title = info;
        }

        /// <summary>
        /// Removes all queued commands.
        /// </summary>
        protected void ClearQueue()
        {
            m_ProcessingQueue.Clear();
        }

        /// <summary>
        /// Hook executed before command processing begins.
        /// </summary>
        public virtual void PreExecute()
        {
        }

        /// <summary>
        /// Hook executed after command processing ends.
        /// </summary>
        public virtual void PostExecute()
        {
        }

        /// <summary>
        /// Executes the next command in the queue.
        /// </summary>
        /// <returns>The informational label for the executed command.</returns>
        public string ExecuteNextCommand()
        {
            var currentUnit = m_ProcessingQueue.Dequeue();

            currentUnit.Action.Invoke();

            return currentUnit.Info;
        }

        /// <summary>
        /// Adds a command to the queue.
        /// </summary>
        /// <param name="action">Action to execute.</param>
        /// <param name="info">Optional description for the action.</param>
        public void AddCommand(Action action, string info = null)
        {
            m_ProcessingQueue.Enqueue(new Command
            {
                Action = action,
                Info = info,
            });
        }

        /// <summary>
        /// Adds a preconstructed command to the queue.
        /// </summary>
        /// <param name="command">The command to enqueue.</param>
        public void AddCommand(Command command)
        {
            m_ProcessingQueue.Enqueue(command);
        }
        #endregion
    }
}
