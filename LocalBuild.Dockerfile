FROM hcr.io/cloud-platform/build:dotnetcore3.1-1.1

ENV GITVERSION_VERSION=5.1.2
ENV LD_LIBRARY_PATH=/root/.dotnet/tools/.store/gitversion.tool/${GITVERSION_VERSION}/gitversion.tool/${GITVERSION_VERSION}/tools/netcoreapp3.0/any/runtimes/debian.9-x64/native/

COPY . /app

WORKDIR /app

RUN dotnet tool restore

RUN dotnet cake ./build/build.cake --bootstrap 
RUN dotnet cake ./build/build.cake --target=GetVersion --exclusive
RUN dotnet cake ./build/build.cake --target=Build --exclusive
RUN dotnet cake ./build/build.cake --target=UnitTests --exclusive
#RUN dotnet cake ./build/build.cake --target=IntegrationTests --exclusive
RUN dotnet cake ./build/build.cake --target=CodeCoverage --exclusive
RUN dotnet cake ./build/build.cake --target=Publish --exclusive --app-version="1.0.0"
#RUN dotnet cake ./build/build.cake --target=BuildImage --exclusive --app-version="1.0.0"