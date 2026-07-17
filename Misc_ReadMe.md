Record a summary of the change to @Localhost_Setup.md similar to other entries. 

Create a new entry in @Localhost_Setup.md that includes details on How it Works, so we can repeat this change in a different fork.
---

docker ps --format "table {{.ID}}\t{{.Names}}\t{{.Ports}}"
CONTAINER ID   NAMES      PORTS
490ec186494a   chat-api   0.0.0.0:80->8

Port 80 is already bound by a previous run of the same chat-api container (490ec186494a) that's still running from an earlier attempt.

Options: docker stop 490ec186494a && docker rm 490ec186494a
docker stop 490ec186494a && docker rm 490ec186494a

Start it again - docker start e258e5611c95 && docker ps --format "table {{.ID}}\t{{.Names}}\t{{.Status}}\t{{.Ports}}"

---
Invoke-RestMethod -Method POST -Uri http://localhost:8000/HoroscopeRegenerateEmbeddings `  -ContentType "application/json" `  -Body '{"password": "admin"}'
---
Check if Azure Functions API host is currently running on port 7071
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:7071/api/Calculate/AddressToGeoLocation/Address/Bhopal --max-time 3 || echo "not reachable"
000 not reachable

Confirmed — the Functions host at port 7071 is down. Start it and the errors should go away.

This is a well-documented local setup. Just start the API host — from the doc:


azurite --location D:\azurite      # in one terminal, if not already running
cd API
dotnet build
func start                          # starts on http://localhost:7071
The earlier server log you pasted (successful AddPerson at 05:54) shows the API was running then and the ERR_CONNECTION_REFUSED errors are from a later attempt where the func start process had stopped or crashed in the meantime.