FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
ARG BUILD_DATE
ARG COMMIT_SHA
ARG BUILD_URL

LABEL org.opencontainers.image.title="Hyland Experience - codegentest" \
 org.opencontainers.image.source="" \
 org.opencontainers.image.documentation="" \
 org.opencontainers.image.created=$BUILD_DATE \
 org.opencontainers.image.revision=$COMMIT_SHA \
 org.opencontainers.image.url=$BUILD_URL

ARG ARTIFACTS_PATH=./.artifacts/dist

ENV ASPNETCORE_URLS=http://+:8080

WORKDIR /app
EXPOSE 8080

COPY ${ARTIFACTS_PATH} /app

RUN useradd -ms /bin/bash -u 1000 api-user
RUN chown -R api-user ./
USER api-user

ENTRYPOINT ["dotnet", "codegentest.dll"]