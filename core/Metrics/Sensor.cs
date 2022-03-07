using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Streamiz.Kafka.Net.Crosscutting;
using Streamiz.Kafka.Net.Metrics.Stats;

namespace Streamiz.Kafka.Net.Metrics
{
    public class Sensor : IEquatable<Sensor>, IComparable<Sensor>
    {
        private readonly Dictionary<MetricName, StreamMetric> metrics;
        private readonly IList<IMeasurableStat> stats;
        protected readonly object @lock = new object();
        private MetricConfig config = new MetricConfig();
        
        public string Name { get; private set; }
        public string Description { get; private set; }
        public MetricsRecordingLevel MetricsRecording { get; private set; }
        public IReadOnlyDictionary<MetricName, StreamMetric> Metrics =>
            new ReadOnlyDictionary<MetricName, StreamMetric>(metrics);

        internal Sensor(
            string name,
            string description,
            MetricsRecordingLevel metricsRecording)
        {
            Name = name;
            Description = description;
            MetricsRecording = metricsRecording;
            metrics = new Dictionary<MetricName, StreamMetric>();
            stats = new List<IMeasurableStat>();
            config.SetRecordingLevel(MetricsRecording);
        }
        
        #region Add

        internal virtual bool AddStatMetric(MetricName name, IMeasurableStat stat, MetricConfig config = null)
        {
            if(!metrics.ContainsKey(name))
            {
                StreamMetric metric = new StreamMetric(name, stat, config ?? this.config);
                metrics.Add(name, metric);
                stats.Add(stat);
                return true;
            }

            return false;
        }

        internal virtual bool AddImmutableMetric<T>(MetricName name, T value, MetricConfig config = null)
        {
            if (!metrics.ContainsKey(name))
            {
                StreamMetric metric = new StreamMetric(name, new ImmutableMetricValue<T>(value), config?? this.config);
                metrics.Add(name, metric);
                if(value is IMeasurableStat)
                    stats.Add((IMeasurableStat)value);
                return true;
            }

            return false;
        }
        
        internal virtual bool AddProviderMetric<T>(MetricName name, Func<T> provider, MetricConfig config = null)
        {
            if (!metrics.ContainsKey(name))
            {
                StreamMetric metric = new StreamMetric(name, new ProviderMetricValue<T>(provider), config?? this.config);
                metrics.Add(name, metric);
                return true;
            }

            return false;
        }

        #endregion

        #region Record

        internal void Record(long value)
            => Record(value, DateTime.Now.GetMilliseconds());
        
        internal void Record(double value, long timeMs)
            => RecordInternal(value, timeMs);
        
        protected virtual void RecordInternal(double value, long timeMs)
        {
            lock (@lock)
            {
                foreach (var stat in stats)
                    stat.Record(config, value, timeMs);
            }
        }
        
        #endregion

        internal void Refresh(long now)
        {
            lock (@lock)
            { 
                foreach(var metric in metrics.Values)
                    metric.Measure(now);
            }
        }
        
        public bool Equals(Sensor? other)
            => other != null && other.Name.Equals(Name);

        public int CompareTo(Sensor? other)
            => other != null ? other.Name.CompareTo(Name) : 1;
        
    }
}