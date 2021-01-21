FROM mcr.microsoft.com/dotnet/sdk:3.1-bionic AS build-env 
COPY . .
WORKDIR /src/StateMachineGithubAction
RUN dotnet restore  /src/StateMachineGithubAction/StateMachineGithubAction.csproj 
RUN dotnet build  /src/StateMachineGithubAction/StateMachineGithubAction.csproj

FROM mcr.microsoft.com/dotnet/runtime:3.1-bionic
COPY --from=build-env /src/StateMachineGithubAction/bin/Debug/netcoreapp3.1 ./app
WORKDIR /app
ENTRYPOINT ["./StateMachineGithubAction"]
