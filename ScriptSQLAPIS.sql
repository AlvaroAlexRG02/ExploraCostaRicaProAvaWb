USE ExploraCostaRicaDB;
GO

INSERT INTO ApiSources (Name, Description, BaseUrl, WebsiteUrl, ContentTypeId, RequiresApiKey, ApiKeySettingName)
SELECT 'Unsplash', 'Fotografías de Unsplash.', 'https://api.unsplash.com/', 'https://unsplash.com',
       (SELECT ContentTypeId FROM ContentTypes WHERE Name = 'Foto'), 1, 'Unsplash:AccessKey'
WHERE NOT EXISTS (SELECT 1 FROM ApiSources WHERE Name = 'Unsplash');

INSERT INTO ApiSources (Name, Description, BaseUrl, WebsiteUrl, ContentTypeId, RequiresApiKey, ApiKeySettingName)
SELECT 'GNews', 'Noticias de GNews.', 'https://gnews.io/api/v4/', 'https://gnews.io',
       (SELECT ContentTypeId FROM ContentTypes WHERE Name = 'Noticia'), 1, 'GNews:ApiKey'
WHERE NOT EXISTS (SELECT 1 FROM ApiSources WHERE Name = 'GNews');
GO

SELECT SourceId, Name, ContentTypeId, ApiKeySettingName FROM ApiSources;

USE ExploraCostaRicaDB;
GO

INSERT INTO ApiSources (Name, Description, BaseUrl, WebsiteUrl, ContentTypeId, RequiresApiKey, ApiKeySettingName)
SELECT 'iNaturalist', 'Observaciones de biodiversidad (fauna y flora).',
       'https://api.inaturalist.org/v1/', 'https://www.inaturalist.org',
       (SELECT ContentTypeId FROM ContentTypes WHERE Name = 'Biodiversidad'), 0, NULL
WHERE NOT EXISTS (SELECT 1 FROM ApiSources WHERE Name = 'iNaturalist');
GO

SELECT SourceId, Name, ContentTypeId, RequiresApiKey FROM ApiSources;
SELECT DestinationId, Name, Latitude, Longitude FROM Destinations;