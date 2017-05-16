# AK/DK
AK/DK is an Akka.NET extension for interacting with, and responding to, the Docker API.

Docker, itself, is an inherently stateful system; anyone can write code to call the Docker API and start a container, but things get more difficult if you want to start a container, wait for it to finish, and then perform another action.
This is where a toolkit like Akka.NET (and AK/DK) can help.

To see what you can do with AK/DK, have a look at the [orchestration engine](examples/orchestration/Program.cs) example or the [test harness](test/AKDK.TestHarness/Program.cs).

---

Note - this is a work-in-progress.

Please feel free to open an issue for questions for feedback; I'm off hiking from May 17th to June 6th 2017 (so apologies in advance if I'm slow to reply), but when I get back it'll be full steam ahead :)
