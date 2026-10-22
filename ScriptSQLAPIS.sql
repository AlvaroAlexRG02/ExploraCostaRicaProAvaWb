USE ExploraCostaRicaDB;
GO


-- ============================================================
-- API 1 - UNSPLASH
-- ============================================================

INSERT INTO ApiSources
(
    Name,
    Description,
    BaseUrl,
    WebsiteUrl,
    ContentTypeId,
    RequiresApiKey,
    ApiKeySettingName
)
SELECT
    'Unsplash',
    'Fotografías de Unsplash.',
    'https://api.unsplash.com/',
    'https://unsplash.com',
    (
        SELECT ContentTypeId
        FROM ContentTypes
        WHERE Name = 'Foto'
    ),
    1,
    'Unsplash:AccessKey'
WHERE NOT EXISTS
(
    SELECT 1
    FROM ApiSources
    WHERE Name = 'Unsplash'
);

GO


-- ============================================================
-- API 2 - GNEWS
-- ============================================================

INSERT INTO ApiSources
(
    Name,
    Description,
    BaseUrl,
    WebsiteUrl,
    ContentTypeId,
    RequiresApiKey,
    ApiKeySettingName
)
SELECT
    'GNews',
    'Noticias de GNews.',
    'https://gnews.io/api/v4/',
    'https://gnews.io',
    (
        SELECT ContentTypeId
        FROM ContentTypes
        WHERE Name = 'Noticia'
    ),
    1,
    'GNews:ApiKey'
WHERE NOT EXISTS
(
    SELECT 1
    FROM ApiSources
    WHERE Name = 'GNews'
);

GO


-- ============================================================
-- API 3 - INATURALIST
-- ============================================================

INSERT INTO ApiSources
(
    Name,
    Description,
    BaseUrl,
    WebsiteUrl,
    ContentTypeId,
    RequiresApiKey,
    ApiKeySettingName
)
SELECT
    'iNaturalist',
    'Observaciones de biodiversidad (fauna y flora).',
    'https://api.inaturalist.org/v1/',
    'https://www.inaturalist.org',
    (
        SELECT ContentTypeId
        FROM ContentTypes
        WHERE Name = 'Biodiversidad'
    ),
    0,
    NULL
WHERE NOT EXISTS
(
    SELECT 1
    FROM ApiSources
    WHERE Name = 'iNaturalist'
);

GO


-- ============================================================
-- COORDENADAS DE DESTINOS
-- Necesarias para consultas geográficas como iNaturalist
-- ============================================================

-- 1 - Volcán Arenal
UPDATE Destinations
SET
    Latitude = 10.463000,
    Longitude = -84.703000
WHERE DestinationId = 1;


-- 2 - Monteverde
UPDATE Destinations
SET
    Latitude = 10.307660,
    Longitude = -84.810590
WHERE DestinationId = 2;


-- 3 - Manuel Antonio
UPDATE Destinations
SET
    Latitude = 9.391172,
    Longitude = -84.145381
WHERE DestinationId = 3;


-- 4 - Tortuguero
UPDATE Destinations
SET
    Latitude = 10.448940,
    Longitude = -83.506830
WHERE DestinationId = 4;


-- 5 - Parque Nacional Corcovado
UPDATE Destinations
SET
    Latitude = 8.582640,
    Longitude = -83.581940
WHERE DestinationId = 5;


-- 6 - Puerto Viejo
UPDATE Destinations
SET
    Latitude = 9.658417,
    Longitude = -82.752778
WHERE DestinationId = 6;


-- 7 - Volcán Poás
UPDATE Destinations
SET
    Latitude = 10.196389,
    Longitude = -84.231667
WHERE DestinationId = 7;


-- 8 - Volcán Irazú
UPDATE Destinations
SET
    Latitude = 9.979000,
    Longitude = -83.852000
WHERE DestinationId = 8;


-- 9 - Rincón de la Vieja
UPDATE Destinations
SET
    Latitude = 10.775323,
    Longitude = -85.349740
WHERE DestinationId = 9;


-- 10 - Guanacaste
-- Se utiliza Liberia como punto de referencia.
UPDATE Destinations
SET
    Latitude = 10.629639,
    Longitude = -85.438253
WHERE DestinationId = 10;

GO


-- ============================================================
-- VERIFICACIÓN DE LAS APIS REGISTRADAS
-- ============================================================

SELECT
    SourceId,
    Name,
    ContentTypeId,
    RequiresApiKey,
    ApiKeySettingName
FROM ApiSources
ORDER BY SourceId;

GO


-- ============================================================
-- VERIFICACIÓN DE COORDENADAS
-- ============================================================

SELECT
    DestinationId,
    Name,
    Latitude,
    Longitude
FROM Destinations
ORDER BY DestinationId;

GO