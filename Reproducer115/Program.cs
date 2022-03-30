using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Streamiz.Kafka.Net;
using Streamiz.Kafka.Net.SerDes;

namespace Reproducer115
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new StreamConfig<StringSerDes, StringSerDes>
            {
                ApplicationId = $"orderToplogyId",
                BootstrapServers = "127.0.0.1:9092",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                NumStreamThreads = 1,
                FollowMetadata = false,
                ApiVersionRequest = false,
                BrokerVersionFallback = "2.4.0",
                Debug = "broker"
            };

            var adminClient = (new AdminClientBuilder(config.ToAdminConfig("create-topic"))).Build();
            var specs = new List<TopicSpecification>
            {
                new TopicSpecification() {Name = "topic"},
                new TopicSpecification() {Name = "pair-topic"}
            };

            try
            {
                await adminClient.CreateTopicsAsync(specs);
                Thread.Sleep(10000); // wait create topic operation is done
            }
            catch (Exception e)
            {
                // if topics already exist, do nothing
            }
            finally
            {
                adminClient.Dispose();
            }
            
            var builder = new StreamBuilder();
            builder
                .Stream<string, string>("topic")
                .Filter((k,v) => v.Length % 2 == 0)
                .To("pair-topic");

            KafkaStream stream = new KafkaStream(builder.Build(), config);
            Console.CancelKeyPress += (o,e) => stream.Dispose();
            
            await stream.StartAsync();

        }
    }
}