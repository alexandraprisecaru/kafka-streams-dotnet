# Reproducer issue #113

## How to start ? 

** Prerequisites**
```
- jq
- docker-compose
- curl
- dotnet 5 (sdk) or dotnet core 3.1 (sdk)
```

Run `./start.sh` to start the reproducer.

Script's workflow :
- start kafka stack with docker-compose
- wait schema registry is UP
- Create topics & internal topics
- [Topology]((./Program.cs)) : Person joins Country to enrich data into a PersonCountry topic
- Deploy multiple avro schema into schema registry (person, country, person-country)
- Set the compatibility to BACKWARD
- Create two country records using Confluent REST Proxy
- Wait 5 seconds
- Create two person records using Confluent REST Proxy
- Build .net streams app
- Run .net streams app in background (be careful to kill -9 this app when you want to stop the demo)
- Start a avro-console-consumer to person-country topic to visualize the join successfully. You have to see two records in the output (maybe wait some seconds)

## How to stop ? 

```
docker-compose down -v
# Stop also .net background process app
```