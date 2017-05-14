# Orchestration

This example shows how AK/DK can be used to build a Docker orchestration engine.

It runs jobs that each consist of a Docker container running `curl` to retrieve content from a target URL, storing that content in a file located in a directory mounted into the container.
Because each container is monitored and managed by a stateful actor, the contents of this file are captured and added to the job store once the container has terminated.
Finally, the container and its temporary file(s) are cleaned up.
