FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Annotate.Web/Annotate.Web.csproj -c Release -o /app/publish --nologo

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir /data && chown app:app /data
USER app
ENV ASPNETCORE_URLS=http://0.0.0.0:24173 \
    Annotate__DataDir=/data \
    Annotate__OpenBrowser=false \
    HOME=/tmp
EXPOSE 24173
ENTRYPOINT ["dotnet", "Annotate.Web.dll"]
