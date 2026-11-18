FROM mcr.microsoft.com/dotnet/aspnet:6.0.9 AS base
WORKDIR /app
RUN apt-get update -qq && apt-get -y install libgdiplus libc6-dev
RUN mkdir /image_write
EXPOSE 8080
ENV ASPNETCORE_URLS=http://*:8080
ENV OutputDirectory /image_write


FROM mcr.microsoft.com/dotnet/sdk:6.0.402 AS build
RUN apt-get update && apt-get install -y procps 
WORKDIR /src
#VOLUME /vol
COPY ["FABTenantD2Api.csproj", "."]
COPY "FABModels.dll" .
RUN dotnet restore "./FABTenantD2Api.csproj" 
COPY . .
WORKDIR "/src/."
#VOLUME "/vol/."
RUN dotnet build "FABTenantD2Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FABTenantD2Api.csproj" -c Release -o /app/publish
#COPY "FAB_Proxy_CA.crt" "/etc/ssl/certs"
#COPY "FAB_Proxy_CA.crt" "/usr/share/ca-certificates/mozilla"
#COPY "FAB_Root.crt" "/etc/ssl/certs"
#COPY "FAB_Root.crt" "/usr/share/ca-certificates/mozilla"
COPY "FAB_Proxy_CA.crt" "/app/publish/"
COPY "FAB_Root.crt" "/app/publish/"

FROM base AS final
RUN addgroup --group dotnetgroup --gid 2000 \
&& adduser --disabled-password \
    --uid 1000 \
    --gid 2000 \
    "dotnetuser"
RUN chown dotnetuser:dotnetgroup /app
RUN usermod -G root dotnetuser



WORKDIR /app
#VOLUME /app
COPY --from=publish /app/publish .
RUN ls
RUN chmod -R 755 /app/wwwroot/Rotativa/wkhtmltopdf
RUN ls "wwwroot/Downloads"
COPY "FAB_Proxy_CA.crt" "/usr/local/share/ca-certificates/"
COPY "FAB_Root.crt" "/usr/local/share/ca-certificates/"
COPY "wwwroot/Downloads/DigiCert_Global_Root_CA.crt" "/usr/local/share/ca-certificates/"
COPY "wwwroot/Downloads/DigiCert_TLS_Hybrid_ECC_SHA384_2020_CA1.crt" "/usr/local/share/ca-certificates/"
RUN chmod 777 /usr/local/share/ca-certificates/DigiCert_Global_Root_CA.crt && update-ca-certificates
RUN chmod 777 /usr/local/share/ca-certificates/FAB_Root.crt && update-ca-certificates
RUN chmod 777 /usr/local/share/ca-certificates/DigiCert_TLS_Hybrid_ECC_SHA384_2020_CA1.crt && update-ca-certificates
RUN chmod 777 /usr/local/share/ca-certificates/FAB_Proxy_CA.crt && update-ca-certificates

USER dotnetuser:dotnetgroup
RUN ls -ltr "/usr/local/share/ca-certificates/"
ENTRYPOINT ["dotnet", "FABTenantD2Api.dll"]