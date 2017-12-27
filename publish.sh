#!/bin/bash
dotnet restore
dotnet publish -c release -o release
rm -rf bin
rm -rf obj