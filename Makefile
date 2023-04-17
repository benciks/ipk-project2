ifeq ($(OS),Windows_NT)
    RUNTIME = win-x64
else ifeq ($(shell uname -s),Linux)
    RUNTIME = linux-x64
else ifeq ($(shell uname -s),Darwin)
    RUNTIME = osx-x64
endif

FRAMEWORK = net6.0
PUBLISH_DIR = $(CURDIR)/src/bin/Debug/$(FRAMEWORK)/$(RUNTIME)/publish
EXECUTABLE = ipk-sniffer

all:
	dotnet publish ./src -f $(FRAMEWORK) -r $(RUNTIME) --no-self-contained
	mv -v $(PUBLISH_DIR)/$(EXECUTABLE) ./$(EXECUTABLE)

zip:
	zip -r xbenci01.zip ipk-sniffer/ipk-sniffer.csproj ipk-sniffer/Program.cs Makefile README.md CHANGELOG.md LICENSE images/

clean:
	rm -rf $(CURDIR)/$(EXECUTABLE)
	rm -rf $(CURDIR)/src/bin
	rm -rf $(CURDIR)/src/obj
