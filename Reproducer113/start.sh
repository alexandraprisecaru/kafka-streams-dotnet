#!/bin/bash

docker-compose up -d

# Wait schema registry is UP
echo "Waiting schema-registry is up ..."

return=$(curl -s http://localhost:8081/subjects | jq '. | length')
while [ "$return" == "" ]; do
  sleep 1
  return=$(curl -s http://localhost:8081/subjects | jq '. | length')
done

echo "schema-registry is up ..."

echo "Create topics"
docker-compose exec broker kafka-topics --bootstrap-server localhost:9092 --topic person --create --partitions 4 --replication-factor 1
docker-compose exec broker kafka-topics --bootstrap-server localhost:9092 --topic country --create --partitions 4 --replication-factor 1
docker-compose exec broker kafka-topics --bootstrap-server localhost:9092 --topic person-country --create --partitions 4 --replication-factor 1
echo "Create internal topics"
# Workaround issue https://github.com/LGouellec/kafka-streams-dotnet/issues/114
docker-compose exec broker kafka-topics --bootstrap-server localhost:9092 --topic test-reproducer-113-KSTREAM-JOINOTHER-0000000013-store-changelog --create --partitions 4 --replication-factor 1
docker-compose exec broker kafka-topics --bootstrap-server localhost:9092 --topic test-reproducer-113-KSTREAM-JOINTHIS-0000000012-store-changelog --create --partitions 4 --replication-factor 1
docker-compose exec broker kafka-topics --bootstrap-server localhost:9092 --topic test-reproducer-113-KSTREAM-KEY-SELECT-0000000001-repartition --create --partitions 4 --replication-factor 1
docker-compose exec broker kafka-topics --bootstrap-server localhost:9092 --topic test-reproducer-113-KSTREAM-KEY-SELECT-0000000003-repartition --create --partitions 4 --replication-factor 1


echo "Deploy person subject"
curl -X POST -H "Content-Type: application/vnd.schemaregistry.v1+json" \
  --data '{"schema": "{\"type\":\"record\",\"name\":\"Person\",\"namespace\":\"avro.bean\",\"fields\":[{\"name\":\"country_id\",\"type\":\"int\"},{\"name\":\"firstName\",\"type\":\"string\"},{\"name\":\"lastName\",\"type\":\"string\"},{\"name\":\"age\",\"type\":\"int\"}]}"}' \
  http://localhost:8081/subjects/person-value/versions

echo ""
echo "Deploy country subject"
curl -X POST -H "Content-Type: application/vnd.schemaregistry.v1+json" \
  --data '{"schema": "{\"type\":\"record\",\"name\":\"Country\",\"namespace\":\"avro.bean\",\"fields\":[{\"name\":\"id\",\"type\":\"int\"},{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"currency\",\"type\":\"string\"},{\"name\":\"PIB\",\"type\":\"float\"}]}"}' \
  http://localhost:8081/subjects/country-value/versions
  
echo ""
echo "Deploy person-country subject"
curl -X POST -H "Content-Type: application/vnd.schemaregistry.v1+json" \
  --data '{"schema": "{\"type\":\"record\",\"name\":\"PersonCountry\",\"namespace\":\"avro.bean\",\"fields\":[{\"name\":\"country\",\"type\":\"string\"},{\"name\":\"firstName\",\"type\":\"string\"},{\"name\":\"lastName\",\"type\":\"string\"},{\"name\":\"age\",\"type\":\"int\"}]}}"}' \
  http://localhost:8081/subjects/person-country-value/versions

echo "Set compatibility to BACKWARD"
curl -X PUT -H "Content-Type: application/vnd.schemaregistry.v1+json" --data '{"compatibility": "BACKWARD"}' http://localhost:8081/config/person-value
curl -X PUT -H "Content-Type: application/vnd.schemaregistry.v1+json" --data '{"compatibility": "BACKWARD"}' http://localhost:8081/config/country-value
curl -X PUT -H "Content-Type: application/vnd.schemaregistry.v1+json" --data '{"compatibility": "BACKWARD"}' http://localhost:8081/config/person-country-value

echo ""
echo "Creating some country"

curl -X POST -H "Content-Type: application/vnd.kafka.avro.v2+json" \
      -H "Accept: application/vnd.kafka.v2+json" \
      --data '{"value_schema": "{\"type\":\"record\",\"name\":\"Country\",\"namespace\":\"avro.bean\",\"fields\":[{\"name\":\"id\",\"type\":\"int\"},{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"currency\",\"type\":\"string\"},{\"name\":\"PIB\",\"type\":\"float\"}]}", "records": [{"value": {"id": 1, "name": "France", "currency": "€", "PIB": 10}}]}' \
      "http://localhost:8082/topics/country"

curl -X POST -H "Content-Type: application/vnd.kafka.avro.v2+json" \
      -H "Accept: application/vnd.kafka.v2+json" \
      --data '{"value_schema": "{\"type\":\"record\",\"name\":\"Country\",\"namespace\":\"avro.bean\",\"fields\":[{\"name\":\"id\",\"type\":\"int\"},{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"currency\",\"type\":\"string\"},{\"name\":\"PIB\",\"type\":\"float\"}]}", "records": [{"value": {"id": 2, "name": "Portugal", "currency": "€", "PIB": 3}}]}' \
      "http://localhost:8082/topics/country"
      
sleep 5

echo ""
echo "Creating some persons"

curl -X POST -H "Content-Type: application/vnd.kafka.avro.v2+json" \
      -H "Accept: application/vnd.kafka.v2+json" \
      --data '{"value_schema": "{\"type\":\"record\",\"name\":\"Person\",\"namespace\":\"avro.bean\",\"fields\":[{\"name\":\"country_id\",\"type\":\"int\"},{\"name\":\"firstName\",\"type\":\"string\"},{\"name\":\"lastName\",\"type\":\"string\"},{\"name\":\"age\",\"type\":\"int\"}]}", "records": [{"value": {"country_id": 1, "firstName": "Sylvain", "lastName": "Test", "age": 28}}]}' \
      "http://localhost:8082/topics/person"

curl -X POST -H "Content-Type: application/vnd.kafka.avro.v2+json" \
      -H "Accept: application/vnd.kafka.v2+json" \
      --data '{"value_schema": "{\"type\":\"record\",\"name\":\"Person\",\"namespace\":\"avro.bean\",\"fields\":[{\"name\":\"country_id\",\"type\":\"int\"},{\"name\":\"firstName\",\"type\":\"string\"},{\"name\":\"lastName\",\"type\":\"string\"},{\"name\":\"age\",\"type\":\"int\"}]}", "records": [{"value": {"country_id": 2, "firstName": "Melissa", "lastName": "Test", "age": 30}}]}' \
      "http://localhost:8082/topics/person"

dotnet build
dotnet run --no-restore --no-build > /tmp/streamiz-log 2>&1 &

docker-compose exec schema-registry kafka-avro-console-consumer --bootstrap-server broker:29092 --topic person-country --from-beginning 