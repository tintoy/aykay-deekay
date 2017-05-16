# Orchestration

This example shows how AK/DK can be used to build a Docker orchestration engine.

It runs jobs that each consist of a Docker container running `curl` to retrieve content from a target URL, storing that content in a file located in a directory mounted into the container.
Because each container is monitored and managed by a stateful actor, the contents of this file are captured and added to the job store once the container has terminated.
Finally, the container and its temporary file(s) are cleaned up.

## Known issues

* You'll need to build the "fetcher" image in the [docker-examples](../docker-examples) directory for this example to work (just run `build-images.sh` or `build-images.ps1`).
* I took some shortcuts when first designing this example, and so it relies a little too heavily on message correlation Ids to identify data (like active jobs) during stateful message exchanges (which makes the logic a little harder to follow that it should be; sorry about that).  
  Instead, we should be identifiying things like jobs using either the job Id or the sender's ActorRef.
