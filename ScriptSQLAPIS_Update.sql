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
-- LOGIN Y USUARIOS
-- ============================================================

IF COL_LENGTH(N'dbo.Users', N'PasswordHash') IS NULL
    ALTER TABLE dbo.Users ADD PasswordHash NVARCHAR(255) NULL;
GO

IF COL_LENGTH(N'dbo.Users', N'Role') IS NULL
    ALTER TABLE dbo.Users ADD [Role] NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Users_Role DEFAULT (N'Usuario');
GO

-- Para volver Admin a un usuario ya registrado (cambie el correo):
-- UPDATE dbo.Users SET [Role] = N'Admin' WHERE Email = N'correo@ejemplo.com';
-- GO


-- ============================================================
-- USUARIOS DE PRUEBA
--   Correo                          Contrasena    Rol
--   admin@exploracostarica.test     Admin1234     Admin
--   usuario1@exploracostarica.test  Prueba1234    Usuario
--   usuario2@exploracostarica.test  Prueba1234    Usuario
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'admin@exploracostarica.test')
    INSERT INTO dbo.Users (Name, Email, IsActive, CreatedAt, PasswordHash, [Role])
    VALUES (N'Admin Prueba', N'admin@exploracostarica.test', 1, SYSDATETIME(), N'AQAAAAIAAYagAAAAENG6AbbVbu64F+/z6mISF2TwwKDDLelZipf6C1lAqIzemHm3yX+FTndX+dTsnWLlGw==', N'Admin');

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'usuario1@exploracostarica.test')
    INSERT INTO dbo.Users (Name, Email, IsActive, CreatedAt, PasswordHash, [Role])
    VALUES (N'Usuario Uno', N'usuario1@exploracostarica.test', 1, SYSDATETIME(), N'AQAAAAIAAYagAAAAEJrclbdgfeHymSrQhwHTQ8TXjMjQYZZZpBPpwM+2P9wz7IuclrWtvBNLaNIPp96DTw==', N'Usuario');

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'usuario2@exploracostarica.test')
    INSERT INTO dbo.Users (Name, Email, IsActive, CreatedAt, PasswordHash, [Role])
    VALUES (N'Usuario Dos', N'usuario2@exploracostarica.test', 1, SYSDATETIME(), N'AQAAAAIAAYagAAAAEJ5AVDZdXg7LDIfLO2ACVcl41g62qkLjaGhQtSS4ecnbS29ExCLmojiaxOaEtHOyuQ==', N'Usuario');

GO


-- ============================================================
-- API 8 - WIKIMEDIA COMMONS
-- ============================================================

INSERT INTO ContentTypes (Name, Description)
SELECT N'Cultura', N'Contenido cultural e historico.'
WHERE NOT EXISTS
(
    SELECT 1
    FROM ContentTypes
    WHERE Name = N'Cultura'
);

GO

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
    'Wikimedia Commons',
    'Imagenes culturales e historicas de Wikimedia Commons (con autor y licencia).',
    'https://commons.wikimedia.org/w/',
    'https://commons.wikimedia.org',
    (
        SELECT ContentTypeId
        FROM ContentTypes
        WHERE Name = 'Cultura'
    ),
    0,
    NULL
WHERE NOT EXISTS
(
    SELECT 1
    FROM ApiSources
    WHERE Name = 'Wikimedia Commons'
);

GO


-- ============================================================
-- API 4 - YOUTUBE DATA API v3 (VIDEOS)
-- La API Key NO se guarda aqui: va en User Secrets
-- con el nombre YouTube:ApiKey.
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
    'YouTube',
    'Videos de destinos, naturaleza y cultura de Costa Rica.',
    'https://www.googleapis.com/youtube/v3/',
    'https://www.youtube.com',
    (
        SELECT ContentTypeId
        FROM ContentTypes
        WHERE Name = 'Video'
    ),
    1,
    'YouTube:ApiKey'
WHERE NOT EXISTS
(
    SELECT 1
    FROM ApiSources
    WHERE Name = 'YouTube'
);

GO


-- ============================================================
-- API 10 - GBIF (BIODIVERSIDAD CIENTIFICA)
-- No requiere API Key.
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
    'GBIF',
    'Registros cientificos de especies observadas en Costa Rica (Global Biodiversity Information Facility).',
    'https://api.gbif.org/v1/',
    'https://www.gbif.org',
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
    WHERE Name = 'GBIF'
);

GO


-- ============================================================
-- API 6 - OPEN LIBRARY (LIBROS)
-- No requiere API Key.
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
    'Open Library',
    'Libros, autores y portadas sobre Costa Rica, su naturaleza y su cultura.',
    'https://openlibrary.org/',
    'https://openlibrary.org',
    (
        SELECT ContentTypeId
        FROM ContentTypes
        WHERE Name = 'Libro'
    ),
    0,
    NULL
WHERE NOT EXISTS
(
    SELECT 1
    FROM ApiSources
    WHERE Name = 'Open Library'
);

GO


-- ============================================================
-- API 7 - CROSSREF (PAPERS)
-- No requiere API Key. El correo para el modo "polite" es
-- opcional y va en User Secrets con el nombre Crossref:Mailto.
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
    'Crossref',
    'Articulos cientificos e investigacion sobre Costa Rica.',
    'https://api.crossref.org/',
    'https://www.crossref.org',
    (
        SELECT ContentTypeId
        FROM ContentTypes
        WHERE Name = 'Paper'
    ),
    0,
    NULL
WHERE NOT EXISTS
(
    SELECT 1
    FROM ApiSources
    WHERE Name = 'Crossref'
);

GO


-- ============================================================
-- DASHBOARD - FORMATO JSON COMUN v1.0

--
-- Crea las tablas Sources y SourceItems.
-- Se puede ejecutar varias veces: no borra ni duplica nada.
--
-- Sources      = una fila por API / fuente.
-- SourceItems  = un contenido por fila. La columna Json guarda el
--                item completo en el formato comun (incluido "data",
--                la respuesta original de la API). El resto de columnas
--                son una copia para filtrar y ordenar sin leer el JSON.
--
-- Las columnas Id, Url, Name, Description, ComponentType y RequiresSecret
-- (Sources) e Id, SourceId, Json y CreatedAt (SourceItems) son las del
-- enunciado del profesor. SourceKey, Category, ItemKey, Title, MediaType
-- y SavedAt se agregan porque el formato comun las necesita.
-- ============================================================


-- Si las tablas ya existen con el DDL base del profesor (sin SourceKey /
-- ItemKey), no se pueden reutilizar tal cual. Si estan vacias, borrelas
-- con:   DROP TABLE dbo.SourceItems;  DROP TABLE dbo.Sources;
-- y vuelva a ejecutar este script.
IF OBJECT_ID(N'dbo.Sources', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Sources', N'SourceKey') IS NULL
    RAISERROR(N'La tabla Sources ya existe sin la columna SourceKey. Borre Sources y SourceItems (si estan vacias) y ejecute de nuevo este script.', 16, 1);
GO

IF OBJECT_ID(N'dbo.SourceItems', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.SourceItems', N'ItemKey') IS NULL
    RAISERROR(N'La tabla SourceItems ya existe sin la columna ItemKey. Borre SourceItems (si esta vacia) y ejecute de nuevo este script.', 16, 1);
GO


-- ============================================================
-- TABLA Sources
-- ============================================================

IF OBJECT_ID(N'dbo.Sources', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sources
    (
        Id             INT IDENTITY(1,1) NOT NULL
                       CONSTRAINT PK_Sources PRIMARY KEY,

        Url            NVARCHAR(500) NOT NULL
                       CONSTRAINT DF_Sources_Url DEFAULT (N''),      -- URL fuente

        Name           NVARCHAR(200) NOT NULL,

        Description    NVARCHAR(500) NULL,

        ComponentType  NVARCHAR(100) NOT NULL
                       CONSTRAINT DF_Sources_ComponentType DEFAULT (N'api'),  -- 'widget','api','feed'

        RequiresSecret BIT NOT NULL
                       CONSTRAINT DF_Sources_RequiresSecret DEFAULT (0),      -- 0 = no, 1 = yes

        -- Formato comun: "id" de la fuente (minusculas, numeros y guiones)
        SourceKey      NVARCHAR(100) NOT NULL,

        -- Formato comun: categoria principal de la fuente
        Category       NVARCHAR(50) NULL
    );
END
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_Sources_SourceKey'
      AND object_id = OBJECT_ID(N'dbo.Sources')
)
    CREATE UNIQUE INDEX UX_Sources_SourceKey
        ON dbo.Sources (SourceKey);
GO


-- ============================================================
-- TABLA SourceItems
-- ============================================================

IF OBJECT_ID(N'dbo.SourceItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SourceItems
    (
        Id        INT IDENTITY(1,1) NOT NULL
                  CONSTRAINT PK_SourceItems PRIMARY KEY,

        SourceId  INT NOT NULL
                  CONSTRAINT FK_SourceItems_Sources
                  FOREIGN KEY REFERENCES dbo.Sources (Id) ON DELETE CASCADE,

        -- Item completo en el formato comun v1.0
        Json      NVARCHAR(MAX) NOT NULL,

        -- Fecha del contenido ("createdAt" del item), en UTC
        CreatedAt DATETIME NOT NULL,

        -- "id" del item dentro de su fuente. BIN2: distingue mayusculas.
        ItemKey   NVARCHAR(250) COLLATE Latin1_General_100_BIN2 NOT NULL,

        Title     NVARCHAR(500) NOT NULL,

        -- text, image, gif, video o audio
        MediaType NVARCHAR(10) NOT NULL,

        -- Categoria propia del item (si es NULL se usa la de la fuente)
        Category  NVARCHAR(50) NULL,

        -- Cuando se guardo en esta base de datos (UTC)
        SavedAt   DATETIME NOT NULL
                  CONSTRAINT DF_SourceItems_SavedAt DEFAULT (GETUTCDATE())
    );
END
GO

-- Un item es unico dentro de su fuente: al importarlo otra vez se reemplaza
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_SourceItems_Source_ItemKey'
      AND object_id = OBJECT_ID(N'dbo.SourceItems')
)
    CREATE UNIQUE INDEX UX_SourceItems_Source_ItemKey
        ON dbo.SourceItems (SourceId, ItemKey);
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_SourceItems_CreatedAt'
      AND object_id = OBJECT_ID(N'dbo.SourceItems')
)
    CREATE INDEX IX_SourceItems_CreatedAt
        ON dbo.SourceItems (CreatedAt);
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_SourceItems_MediaType'
      AND object_id = OBJECT_ID(N'dbo.SourceItems')
)
    CREATE INDEX IX_SourceItems_MediaType
        ON dbo.SourceItems (MediaType);
GO


-- Verificacion rapida
SELECT
    (SELECT COUNT(*) FROM dbo.Sources)     AS Fuentes,
    (SELECT COUNT(*) FROM dbo.SourceItems) AS Items;
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