﻿using Confluent.Kafka;

namespace Kafka.Streams.Net.Processors.Internal
{
    internal abstract class ExtractRecordMetadataTimestamp : ITimestampExtractor
    {
        public long Extract(ConsumeResult<object, object> record, long partitionTime)
        {
            if (record.Timestamp.UnixTimestampMs < 0)
            {
                return onInvalidTimestamp(record, record.Timestamp.UnixTimestampMs, partitionTime);
            }

            return record.Timestamp.UnixTimestampMs;
        }

        public abstract long onInvalidTimestamp(ConsumeResult<object, object> record, long recordTimestamp, long partitionTime);
    }
}