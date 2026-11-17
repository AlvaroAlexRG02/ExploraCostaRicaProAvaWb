/* ============================================================
   PROYECTO: EXPLORA COSTA RICA
   BASE DE DATOS: ExploraCostaRicaDB

   OBJETIVO:
   Base de datos principal del aplicativo web.

   Permitirá manejar:
   - Usuarios
   - Tipos de contenido
   - Categorías
   - Destinos de Costa Rica
   - Fuentes / APIs externas
   - Contenido multimedia
   - Favoritos
   - Descargas
   - Historial de búsquedas
   - Etiquetas

   Las APIs externas NO se guardarán completas.
   Solo almacena el contenido que la aplicación necesite
   conservar, por ejemplo favoritos o elementos descargados.
   ============================================================ */


USE master;
GO


/* ============================================================
   1. CREAR BASE DE DATOS
   ============================================================ */

IF DB_ID('ExploraCostaRicaDB') IS NULL
BEGIN
    CREATE DATABASE ExploraCostaRicaDB;
END
GO


USE ExploraCostaRicaDB;
GO



IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
BEGIN
    RAISERROR(N'La base ExploraCostaRicaDB ya tiene tablas. Este script es solo para una base nueva. En una base existente ejecute unicamente ScriptSQLAPIS.sql.', 16, 1);
    SET NOEXEC ON;
END
GO


/* ============================================================
   2. TABLA: USERS
   ------------------------------------------------------------
   Guarda los usuarios registrados en el aplicativo.

   Más adelante permitirá:
   - Login
   - Favoritos personalizados
   - Historial
   - Descargas
   ============================================================ */

CREATE TABLE Users
(
    UserId INT IDENTITY(1,1) PRIMARY KEY,

    Name NVARCHAR(100) NOT NULL,

    Email NVARCHAR(150) NOT NULL,

    CreatedAt DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    IsActive BIT NOT NULL
        DEFAULT 1,

    CONSTRAINT UQ_Users_Email
        UNIQUE (Email)
);
GO


/* ============================================================
   3. TABLA: CONTENT TYPES
   ------------------------------------------------------------
   Indica QUÉ tipo de contenido estamos manejando.

   Ejemplos:
   - Foto
   - Video
   - Noticia
   - Clima
   - Libro
   - Paper
   ============================================================ */

CREATE TABLE ContentTypes
(
    ContentTypeId INT IDENTITY(1,1) PRIMARY KEY,

    Name NVARCHAR(80) NOT NULL,

    Description NVARCHAR(300) NULL,


    Icon NVARCHAR(100) NULL,

    IsActive BIT NOT NULL
        DEFAULT 1,

    CONSTRAINT UQ_ContentTypes_Name
        UNIQUE (Name)
);
GO


/* ============================================================
   4. TABLA: CATEGORIES
   ------------------------------------------------------------
   Clasifica el contenido según su temática.

   Ejemplos:
   - Naturaleza
   - Playas
   - Aventura
   - Cultura
   - Turismo
   ============================================================ */

CREATE TABLE Categories
(
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,

    Name NVARCHAR(100) NOT NULL,

    Description NVARCHAR(500) NULL,

    Icon NVARCHAR(100) NULL,

    IsActive BIT NOT NULL
        DEFAULT 1,

    CONSTRAINT UQ_Categories_Name
        UNIQUE (Name)
);
GO


/* ============================================================
   5. TABLA: DESTINATIONS
   ------------------------------------------------------------
   Almacena destinos y lugares de Costa Rica.

   Nos servirá para:
   - Pantalla de destinos
   - Filtros
   - Búsqueda
   - Contenido relacionado
   - Botón "Sorpréndeme"
   ============================================================ */

CREATE TABLE Destinations
(
    DestinationId INT IDENTITY(1,1) PRIMARY KEY,

    Name NVARCHAR(150) NOT NULL,

    Province NVARCHAR(100) NOT NULL,

    Description NVARCHAR(1000) NULL,


    Latitude DECIMAL(9,6) NULL,

    Longitude DECIMAL(9,6) NULL,

    ImageUrl NVARCHAR(1500) NULL,


    IsFeatured BIT NOT NULL
        DEFAULT 0,

    IsActive BIT NOT NULL
        DEFAULT 1,

    CreatedAt DATETIME2 NOT NULL
        DEFAULT SYSDATETIME()
);
GO


/* ============================================================
   6. TABLA: API SOURCES
   ------------------------------------------------------------
   Registra las diferentes APIs o fuentes externas.

   Ejemplos futuros:
   - API de fotografías
   - API de videos
   - API de noticias
   - API del clima
   - API de biodiversidad

   IMPORTANTE:
   NO guardaremos las API Keys directamente en la base.
   ============================================================ */

CREATE TABLE ApiSources
(
    SourceId INT IDENTITY(1,1) PRIMARY KEY,

    Name NVARCHAR(150) NOT NULL,

    Description NVARCHAR(500) NULL,

    BaseUrl NVARCHAR(1000) NULL,

    WebsiteUrl NVARCHAR(1000) NULL,

    ContentTypeId INT NULL,

    RequiresApiKey BIT NOT NULL
        DEFAULT 0,


    ApiKeySettingName NVARCHAR(200) NULL,

    IsActive BIT NOT NULL
        DEFAULT 1,

    CreatedAt DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    CONSTRAINT UQ_ApiSources_Name
        UNIQUE (Name),

    CONSTRAINT FK_ApiSources_ContentTypes
        FOREIGN KEY (ContentTypeId)
        REFERENCES ContentTypes(ContentTypeId)
);
GO


/* ============================================================
   7. TABLA: MEDIA ITEMS
   ------------------------------------------------------------
   Esta es una de las tablas principales.

   Guarda contenido procedente de diferentes APIs usando
   un formato común.

   Puede representar:
   - Foto
   - Video
   - Noticia
   - Libro
   - Paper
   - Actividad
   - Información de biodiversidad
   ============================================================ */

CREATE TABLE MediaItems
(
    MediaId BIGINT IDENTITY(1,1) PRIMARY KEY,

    /* Identificador original del contenido en la API */
    ExternalId NVARCHAR(250) NULL,

    /* De qué API o fuente provino */
    SourceId INT NOT NULL,

    /* Foto, video, noticia, etc. */
    ContentTypeId INT NOT NULL,

    /* Naturaleza, turismo, cultura, etc. */
    CategoryId INT NULL,

    /* Destino relacionado */
    DestinationId INT NULL,

    Title NVARCHAR(300) NOT NULL,

    Description NVARCHAR(MAX) NULL,

    /* Enlace original del contenido */
    Url NVARCHAR(1500) NULL,

    /* Imagen que mostraremos en una tarjeta */
    ImageUrl NVARCHAR(1500) NULL,

    Author NVARCHAR(200) NULL,

    PublishedAt DATETIME2 NULL,

    /* Fecha en que nuestra app lo almacenó */
    SavedAt DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    IsActive BIT NOT NULL
        DEFAULT 1,

    CONSTRAINT FK_MediaItems_ApiSources
        FOREIGN KEY (SourceId)
        REFERENCES ApiSources(SourceId),

    CONSTRAINT FK_MediaItems_ContentTypes
        FOREIGN KEY (ContentTypeId)
        REFERENCES ContentTypes(ContentTypeId),

    CONSTRAINT FK_MediaItems_Categories
        FOREIGN KEY (CategoryId)
        REFERENCES Categories(CategoryId),

    CONSTRAINT FK_MediaItems_Destinations
        FOREIGN KEY (DestinationId)
        REFERENCES Destinations(DestinationId)
);
GO


/* ============================================================
   Evita guardar dos veces el mismo contenido proveniente
   de una misma API.
   ============================================================ */

CREATE UNIQUE INDEX UX_MediaItems_Source_ExternalId
ON MediaItems(SourceId, ExternalId)
WHERE ExternalId IS NOT NULL;
GO


/* ============================================================
   8. TABLA: FAVORITES
   ------------------------------------------------------------
   Guarda qué contenido ha marcado como favorito un usuario.

   No duplicamos toda la información.
   Solo relacionamos:
   Usuario → MediaItem
   ============================================================ */

CREATE TABLE Favorites
(
    FavoriteId BIGINT IDENTITY(1,1) PRIMARY KEY,

    UserId INT NOT NULL,

    MediaId BIGINT NOT NULL,

    CreatedAt DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Favorites_Users
        FOREIGN KEY (UserId)
        REFERENCES Users(UserId),

    CONSTRAINT FK_Favorites_MediaItems
        FOREIGN KEY (MediaId)
        REFERENCES MediaItems(MediaId),

    /* Impide guardar dos veces el mismo favorito */
    CONSTRAINT UQ_Favorites_User_Media
        UNIQUE (UserId, MediaId)
);
GO


/* ============================================================
   9. TABLA: DOWNLOADS
   ------------------------------------------------------------
   Guarda un historial de los elementos exportados.

   Ejemplos:
   - JSON
   - PDF
   - CSV

   Nos servirá para la funcionalidad de descarga/intercambio.
   ============================================================ */

CREATE TABLE Downloads
(
    DownloadId BIGINT IDENTITY(1,1) PRIMARY KEY,

    UserId INT NOT NULL,

    MediaId BIGINT NOT NULL,

    FileFormat NVARCHAR(20) NOT NULL,

    FileName NVARCHAR(300) NULL,

    DownloadedAt DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Downloads_Users
        FOREIGN KEY (UserId)
        REFERENCES Users(UserId),

    CONSTRAINT FK_Downloads_MediaItems
        FOREIGN KEY (MediaId)
        REFERENCES MediaItems(MediaId)
);
GO


/* ============================================================
   10. TABLA: SEARCH HISTORY
   ------------------------------------------------------------
   Guarda las búsquedas realizadas.

   En el futuro puede servir para:
   - Búsquedas recientes
   - Estadísticas
   - Recomendaciones
   ============================================================ */

CREATE TABLE SearchHistory
(
    SearchId BIGINT IDENTITY(1,1) PRIMARY KEY,

    /* Puede ser NULL si todavía no existe Login */
    UserId INT NULL,

    SearchText NVARCHAR(300) NOT NULL,

    CategoryId INT NULL,

    DestinationId INT NULL,

    ResultCount INT NULL,

    SearchedAt DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    CONSTRAINT FK_SearchHistory_Users
        FOREIGN KEY (UserId)
        REFERENCES Users(UserId),

    CONSTRAINT FK_SearchHistory_Categories
        FOREIGN KEY (CategoryId)
        REFERENCES Categories(CategoryId),

    CONSTRAINT FK_SearchHistory_Destinations
        FOREIGN KEY (DestinationId)
        REFERENCES Destinations(DestinationId)
);
GO


/* ============================================================
   11. TABLA: TAGS
   ------------------------------------------------------------
   Palabras clave relacionadas con el contenido.

   Ejemplos:
   - Volcán
   - Playa
   - Bosque
   - Fauna
   - Senderismo
   ============================================================ */

CREATE TABLE Tags
(
    TagId INT IDENTITY(1,1) PRIMARY KEY,

    Name NVARCHAR(100) NOT NULL,

    CONSTRAINT UQ_Tags_Name
        UNIQUE (Name)
);
GO


/* ============================================================
   12. TABLA: MEDIA TAGS
   ------------------------------------------------------------
   Relación MUCHOS A MUCHOS.

   Un contenido puede tener muchos Tags.
   Un Tag puede pertenecer a muchos contenidos.

   Ejemplo:

   Volcán Arenal
       → Volcán
       → Naturaleza
       → Aventura
       → Turismo
   ============================================================ */

CREATE TABLE MediaTags
(
    MediaId BIGINT NOT NULL,

    TagId INT NOT NULL,

    CONSTRAINT PK_MediaTags
        PRIMARY KEY (MediaId, TagId),

    CONSTRAINT FK_MediaTags_MediaItems
        FOREIGN KEY (MediaId)
        REFERENCES MediaItems(MediaId),

    CONSTRAINT FK_MediaTags_Tags
        FOREIGN KEY (TagId)
        REFERENCES Tags(TagId)
);
GO


/* ============================================================
   13. ÍNDICES
   ------------------------------------------------------------
   Mejoran el rendimiento cuando hagamos búsquedas o filtros.
   ============================================================ */

CREATE INDEX IX_MediaItems_CategoryId
ON MediaItems(CategoryId);
GO

CREATE INDEX IX_MediaItems_DestinationId
ON MediaItems(DestinationId);
GO

CREATE INDEX IX_MediaItems_SourceId
ON MediaItems(SourceId);
GO

CREATE INDEX IX_Favorites_UserId
ON Favorites(UserId);
GO

CREATE INDEX IX_Downloads_UserId
ON Downloads(UserId);
GO

CREATE INDEX IX_SearchHistory_UserId
ON SearchHistory(UserId);
GO


/* ============================================================
   14. DATOS INICIALES
   ============================================================ */


/* ============================================================
   Usuario temporal para desarrollo.

   Mientras no tengamos Login utilizaremos este usuario.
   ============================================================ */

INSERT INTO Users
(
    Name,
    Email
)
VALUES
(
    'Usuario Demo',
    'demo@exploracostarica.local'
);
GO


/* ============================================================
   TIPOS DE CONTENIDO
   ============================================================ */

INSERT INTO ContentTypes
(
    Name,
    Description,
    Icon
)
VALUES
(
    'Foto',
    'Fotografías e imágenes',
    'image'
),
(
    'Video',
    'Videos relacionados con Costa Rica',
    'video'
),
(
    'Noticia',
    'Noticias y actualidad',
    'newspaper'
),
(
    'Clima',
    'Información meteorológica',
    'cloud-sun'
),
(
    'Lugar',
    'Lugares y puntos de interés',
    'map-pin'
),
(
    'Libro',
    'Libros y material bibliográfico',
    'book'
),
(
    'Paper',
    'Artículos académicos e investigación',
    'file-text'
),
(
    'Biodiversidad',
    'Flora, fauna y especies',
    'leaf'
),
(
    'Actividad',
    'Eventos y actividades',
    'calendar'
),
(
    'Cultura',
    'Contenido cultural e histórico',
    'landmark'
);
GO


/* ============================================================
   CATEGORÍAS
   ============================================================ */

INSERT INTO Categories
(
    Name,
    Description,
    Icon
)
VALUES
(
    'Naturaleza',
    'Parques, bosques, volcanes y naturaleza',
    'trees'
),
(
    'Playas',
    'Playas y zonas costeras',
    'waves'
),
(
    'Aventura',
    'Actividades de aventura',
    'mountain'
),
(
    'Cultura',
    'Historia, cultura y tradiciones',
    'landmark'
),
(
    'Turismo',
    'Información turística',
    'map'
),
(
    'Biodiversidad',
    'Fauna y flora',
    'leaf'
),
(
    'Gastronomía',
    'Comida y gastronomía costarricense',
    'utensils'
),
(
    'Deportes',
    'Actividades y noticias deportivas',
    'activity'
),
(
    'Investigación',
    'Información científica y académica',
    'microscope'
),
(
    'Eventos',
    'Eventos y actividades locales',
    'calendar'
);
GO


/* ============================================================
   DESTINOS INICIALES
   ============================================================ */

INSERT INTO Destinations
(
    Name,
    Province,
    Description,
    IsFeatured
)
VALUES
(
    'Volcán Arenal',
    'Alajuela',
    'Destino natural reconocido por su volcán, aguas termales y actividades de aventura.',
    1
),
(
    'Monteverde',
    'Puntarenas',
    'Zona reconocida por su bosque nuboso y biodiversidad.',
    1
),
(
    'Manuel Antonio',
    'Puntarenas',
    'Parque nacional conocido por sus playas y vida silvestre.',
    1
),
(
    'Tortuguero',
    'Limón',
    'Zona de canales, bosque tropical y anidación de tortugas.',
    1
),
(
    'Parque Nacional Corcovado',
    'Puntarenas',
    'Una de las regiones con mayor biodiversidad de Costa Rica.',
    0
),
(
    'Puerto Viejo',
    'Limón',
    'Destino caribeño reconocido por sus playas y cultura.',
    1
),
(
    'Volcán Poás',
    'Alajuela',
    'Uno de los volcanes más visitados del país.',
    0
),
(
    'Volcán Irazú',
    'Cartago',
    'Volcán de gran altitud ubicado en la provincia de Cartago.',
    0
),
(
    'Rincón de la Vieja',
    'Guanacaste',
    'Parque nacional con actividad volcánica y senderos naturales.',
    0
),
(
    'Guanacaste',
    'Guanacaste',
    'Región conocida por sus playas, cultura y turismo.',
    1
);
GO


/* ============================================================
   TAGS INICIALES
   ============================================================ */

INSERT INTO Tags
(
    Name
)
VALUES
('Volcán'),
('Playa'),
('Bosque'),
('Fauna'),
('Flora'),
('Senderismo'),
('Aventura'),
('Cultura'),
('Caribe'),
('Pacífico'),
('Turismo'),
('Fotografía');
GO


/* ============================================================
   FUENTE TEMPORAL

   Mientras no conectemos una API real utilizaremos esta
   fuente para probar la página.
   ============================================================ */

INSERT INTO ApiSources
(
    Name,
    Description,
    ContentTypeId,
    RequiresApiKey
)
VALUES
(
    'Contenido Local Demo',
    'Contenido temporal utilizado durante el desarrollo inicial de Explora Costa Rica.',
    1,
    0
);
GO


/* ============================================================
   CONTENIDO DE PRUEBA

   Esto nos permitirá comprobar inmediatamente que
   Visual Studio puede leer la base de datos.
   ============================================================ */

INSERT INTO MediaItems
(
    ExternalId,
    SourceId,
    ContentTypeId,
    CategoryId,
    DestinationId,
    Title,
    Description
)
VALUES
(
    'DEMO-001',
    1,      -- Contenido Local Demo
    1,      -- Foto
    1,      -- Naturaleza
    1,      -- Volcán Arenal
    'Volcán Arenal',
    'Uno de los destinos naturales más conocidos de Costa Rica.'
),
(
    'DEMO-002',
    1,      -- Contenido Local Demo
    2,      -- Video
    1,      -- Naturaleza
    2,      -- Monteverde
    'Monteverde',
    'Bosque nuboso, naturaleza y aventura.'
),
(
    'DEMO-003',
    1,      -- Contenido Local Demo
    3,      -- Noticia
    5,      -- Turismo
    10,     -- Guanacaste
    'Turismo en Guanacaste',
    'Información turística sobre la provincia de Guanacaste.'
);
GO


/* ============================================================
   RELACIONAR MEDIA ITEMS CON TAGS
   ============================================================ */

INSERT INTO MediaTags
(
    MediaId,
    TagId
)
VALUES
(1, 1),     -- Arenal → Volcán
(1, 7),     -- Arenal → Aventura
(1, 11),    -- Arenal → Turismo

(2, 3),     -- Monteverde → Bosque
(2, 4),     -- Monteverde → Fauna
(2, 5),     -- Monteverde → Flora

(3, 10),    -- Guanacaste → Pacífico
(3, 11);    -- Guanacaste → Turismo
GO


/* ============================================================
   15. VISTA: MEDIA CATALOG
   ------------------------------------------------------------
   Esta vista reúne la información necesaria para mostrar
   las tarjetas del catálogo.

   En vez de consultar muchas tablas individualmente,
   podremos consultar:

       SELECT * FROM vw_MediaCatalog
   ============================================================ */

CREATE VIEW vw_MediaCatalog
AS

SELECT
    M.MediaId,
    M.ExternalId,

    M.Title,
    M.Description,

    M.Url,
    M.ImageUrl,

    M.Author,
    M.PublishedAt,
    M.SavedAt,

    CT.ContentTypeId,
    CT.Name AS ContentType,

    C.CategoryId,
    C.Name AS Category,

    D.DestinationId,
    D.Name AS Destination,
    D.Province,

    S.SourceId,
    S.Name AS Source

FROM MediaItems M

INNER JOIN ContentTypes CT
    ON M.ContentTypeId = CT.ContentTypeId

INNER JOIN ApiSources S
    ON M.SourceId = S.SourceId

LEFT JOIN Categories C
    ON M.CategoryId = C.CategoryId

LEFT JOIN Destinations D
    ON M.DestinationId = D.DestinationId

WHERE M.IsActive = 1;
GO


/* ============================================================
   16. VISTA: USER FAVORITES
   ------------------------------------------------------------
   Devuelve los favoritos de los usuarios junto con
   toda la información necesaria para mostrarlos.
   ============================================================ */

CREATE VIEW vw_UserFavorites
AS

SELECT
    F.FavoriteId,

    F.UserId,

    U.Name AS UserName,

    M.MediaId,

    M.Title,
    M.Description,

    M.Url,
    M.ImageUrl,

    CT.Name AS ContentType,

    C.Name AS Category,

    D.Name AS Destination,

    S.Name AS Source,

    F.CreatedAt AS FavoriteDate

FROM Favorites F

INNER JOIN Users U
    ON F.UserId = U.UserId

INNER JOIN MediaItems M
    ON F.MediaId = M.MediaId

INNER JOIN ContentTypes CT
    ON M.ContentTypeId = CT.ContentTypeId

INNER JOIN ApiSources S
    ON M.SourceId = S.SourceId

LEFT JOIN Categories C
    ON M.CategoryId = C.CategoryId

LEFT JOIN Destinations D
    ON M.DestinationId = D.DestinationId;
GO


/* ============================================================
   17. CONSULTAS DE PRUEBA
   ------------------------------------------------------------
   Estas consultas permiten comprobar que todo se creó.
   ============================================================ */


PRINT '===========================================';
PRINT 'BASE DE DATOS CREADA CORRECTAMENTE';
PRINT 'ExploraCostaRicaDB';
PRINT '===========================================';
GO


/* Usuarios */
SELECT * FROM Users;
GO


/* Tipos de contenido */
SELECT * FROM ContentTypes;
GO


/* Categorías */
SELECT * FROM Categories;
GO


/* Destinos */
SELECT * FROM Destinations;
GO


/* Fuentes */
SELECT * FROM ApiSources;
GO


/* Contenido */
SELECT * FROM MediaItems;
GO


/* Etiquetas */
SELECT * FROM Tags;
GO


/* Catálogo completo */
SELECT * FROM vw_MediaCatalog;
GO


/* ############################################################
   PARTE 2 - ACTUALIZACIONES (antes ScriptSQLAPIS.sql)
   APIs, coordenadas, login, usuarios de prueba, Wikimedia
   y dashboard. Se puede ejecutar varias veces.
   ############################################################ */

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
-- Agrega a Users las columnas que necesita el inicio de sesion:
--   PasswordHash = hash de la contrasena (nunca texto plano).
--                  NULL en usuarios sin contrasena (ej. el usuario demo).
--   Role         = 'Usuario' (por defecto) o 'Admin'.
-- Se puede ejecutar varias veces.
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
-- USUARIOS DE PRUEBA (solo para desarrollo y demo)
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


/* Fin de la proteccion (si estaba activada, vuelve a la normalidad) */
SET NOEXEC OFF;
GO
