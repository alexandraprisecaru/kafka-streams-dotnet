using System;
using Confluent.Kafka;
using Streamiz.Kafka.Net;
using Streamiz.Kafka.Net.SerDes;
using Streamiz.Kafka.Net.Stream;
using System.IO;
using System.Threading.Tasks;
using avro.bean;
using Microsoft.Extensions.Logging;
using Streamiz.Kafka.Net.SchemaRegistry.SerDes.Avro;

namespace Reproducer113
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new StreamConfig<Int32SerDes, StringSerDes>();
            config.ApplicationId = "test-reproducer-113";
            config.BootstrapServers = "localhost:9092";
            config.SchemaRegistryUrl = "http://localhost:8081";
            config.CommitIntervalMs = (long)TimeSpan.FromSeconds(5).TotalMilliseconds;
            config.AutoOffsetReset = AutoOffsetReset.Earliest;
            config.StateDir = Path.Combine(".", Guid.NewGuid().ToString());
            config.AutoRegisterSchemas = true;
            config.Logger = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddConsole();
            });
            
            StreamBuilder builder = new StreamBuilder();

            var countryStream = builder
                .Stream<String, Country, StringSerDes, SchemaAvroSerDes<Country>>("country")
                .SelectKey((k, v) => v.id);
     
            builder.Stream<String, Person, StringSerDes, SchemaAvroSerDes<Person>>("person")
                .SelectKey((k,v) => v.country_id)
                .Join<Country, PersonCountry, SchemaAvroSerDes<Country>>(countryStream, (person, country) =>
                {
                    PersonCountry p = new PersonCountry();
                    p.age = person.age;
                    p.country = country.name;
                    p.firstName = person.firstName;
                    p.lastName = person.lastName;
                    return p;
                }, 
                    JoinWindowOptions.Of(TimeSpan.FromMinutes(5)),
                    StreamJoinProps.With(
                        new Int32SerDes(),
                        new SchemaAvroSerDes<Country>(),
                        new SchemaAvroSerDes<PersonCountry>()))
                .To<Int32SerDes, SchemaAvroSerDes<PersonCountry>>("person-country");
            
            Topology t = builder.Build();
            KafkaStream stream = new KafkaStream(t, config);
            
            Console.CancelKeyPress += (o, e) => stream.Dispose();

            await stream.StartAsync();
        }
    }
}