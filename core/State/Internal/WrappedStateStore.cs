﻿using System;
using System.Linq;
using Confluent.Kafka;
using Streamiz.Kafka.Net.Processors;
using Streamiz.Kafka.Net.Processors.Internal;

namespace Streamiz.Kafka.Net.State.Internal
{
    internal interface IWrappedStateStore
    {
        IStateStore Wrapped { get; }
    }

    internal class WrappedStore
    {
        internal static bool IsTimestamped(IStateStore stateStore)
        {
            if (stateStore is ITimestampedStore)
                return true;
            else if (stateStore is IWrappedStateStore)
                return IsTimestamped(((IWrappedStateStore)stateStore).Wrapped);
            else
                return false;
        }
    }

    internal class WrappedStateStore<S> : IStateStore, IWrappedStateStore
        where S : IStateStore
    {
        protected ProcessorContext context;
        protected readonly S wrapped;
        protected string changelogTopic;

        public WrappedStateStore(S wrapped)
        {
            this.wrapped = wrapped;
        }

        #region StateStore Impl

        public virtual string Name => wrapped.Name;

        public virtual bool Persistent => wrapped.Persistent;

        public virtual bool IsOpen => wrapped.IsOpen;

        public virtual void Close() => wrapped.Close();

        public virtual void Flush() => wrapped.Flush();

        public virtual void Init(ProcessorContext context, IStateStore root)
        {
            this.context = context;
            
            changelogTopic = context.ChangelogFor(Name);
            if (string.IsNullOrEmpty(changelogTopic))
                changelogTopic = ProcessorStateManager.StoreChangelogTopic(context.ApplicationId, Name);
            
            wrapped.Init(context, root);
        }

        #endregion

        public IStateStore Wrapped => wrapped;

        protected SerializationContext GetSerializationContext(bool isKey, Type topicType = null)
        {
            string intermediateTopic = context?.JoinIntermediateTopics?.FirstOrDefault(x =>
                x.Value == topicType || 
                topicType is { IsGenericType: true } && topicType.GetGenericArguments().First() == x.Value).Key;

            string topic = !string.IsNullOrWhiteSpace(intermediateTopic)
                ? intermediateTopic
                : context?.RecordContext?.Topic;

            return new SerializationContext(isKey ? MessageComponentType.Key : MessageComponentType.Value,
                changelogTopic,
                context?.RecordContext?.Headers);
        }
    }
}
