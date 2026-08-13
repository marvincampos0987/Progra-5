IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Articulos] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(max) NOT NULL,
    [Descripcion] nvarchar(max) NOT NULL,
    [Tipo] int NOT NULL,
    [Precio] int NOT NULL,
    [Valor] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Articulos] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Logros] (
    [Id] int NOT NULL IDENTITY,
    [Titulo] nvarchar(max) NOT NULL,
    [Descripcion] nvarchar(max) NOT NULL,
    [PuntosExperienciaRecompensa] int NOT NULL,
    CONSTRAINT [PK_Logros] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Usuarios] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [PasswordHash] varbinary(max) NOT NULL,
    [PasswordSalt] varbinary(max) NOT NULL,
    [AvatarUrl] nvarchar(max) NOT NULL,
    [ColorPerfil] nvarchar(max) NOT NULL,
    [AvatarBorde] nvarchar(max) NOT NULL,
    [AspectoJuego] nvarchar(max) NOT NULL,
    [Monedas] bigint NOT NULL,
    [Nivel] int NOT NULL,
    [ExperienciaActual] int NOT NULL,
    [UltimaRecompensaDiaria] datetime2 NULL,
    CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [HistorialPartidas] (
    [Id] int NOT NULL IDENTITY,
    [UsuarioId] int NOT NULL,
    [Juego] int NOT NULL,
    [Resultado] int NOT NULL,
    [MonedasApostadas] int NOT NULL,
    [MonedasGanadasOPerdidas] int NOT NULL,
    [FechaPartida] datetime2 NOT NULL,
    CONSTRAINT [PK_HistorialPartidas] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_HistorialPartidas_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [UsuarioArticulos] (
    [UsuarioId] int NOT NULL,
    [ArticuloId] int NOT NULL,
    [FechaCompra] datetime2 NOT NULL,
    [Equipado] bit NOT NULL,
    CONSTRAINT [PK_UsuarioArticulos] PRIMARY KEY ([UsuarioId], [ArticuloId]),
    CONSTRAINT [FK_UsuarioArticulos_Articulos_ArticuloId] FOREIGN KEY ([ArticuloId]) REFERENCES [Articulos] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UsuarioArticulos_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [UsuarioLogros] (
    [UsuarioId] int NOT NULL,
    [LogroId] int NOT NULL,
    [FechaDesbloqueo] datetime2 NOT NULL,
    CONSTRAINT [PK_UsuarioLogros] PRIMARY KEY ([UsuarioId], [LogroId]),
    CONSTRAINT [FK_UsuarioLogros_Logros_LogroId] FOREIGN KEY ([LogroId]) REFERENCES [Logros] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UsuarioLogros_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_HistorialPartidas_UsuarioId] ON [HistorialPartidas] ([UsuarioId]);
GO

CREATE INDEX [IX_UsuarioArticulos_ArticuloId] ON [UsuarioArticulos] ([ArticuloId]);
GO

CREATE INDEX [IX_UsuarioLogros_LogroId] ON [UsuarioLogros] ([LogroId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260813043008_InitialCreate', N'8.0.11');
GO

COMMIT;
GO

-- Seed Data Initialization
IF NOT EXISTS (SELECT 1 FROM [Logros])
BEGIN
    INSERT INTO [Logros] ([Titulo], [Descripcion], [PuntosExperienciaRecompensa]) VALUES
    (N'Primera Racha', N'Ganar 5 partidas', 100),
    (N'Jugador Experimentado', N'Llegar al nivel 10', 200);
END;
GO

IF NOT EXISTS (SELECT 1 FROM [Articulos])
BEGIN
    INSERT INTO [Articulos] ([Nombre], [Descripcion], [Tipo], [Precio], [Valor]) VALUES
    (N'Borde Fuego', N'Un avatar rodeado de llamas vivientes', 0, 500, N'fire_border_url'),
    (N'Borde Neon', N'Luces LED retro-futuristas', 0, 300, N'neon_border_url'),
    (N'Fichas de Obsidiana', N'Un elegante aspecto de roca volcánica oscura para Dominó', 1, 1200, N'obsidian_skin'),
    (N'Fichas Cyberpunk', N'Aspecto futurista de neón cibernético', 1, 1500, N'cyberpunk_skin'),
    (N'Reacción: Risas', N'Audio de risa corta para enviar a tus oponentes', 2, 100, N'laugh_sound');
END;
GO

IF NOT EXISTS (SELECT 1 FROM [Usuarios] WHERE [Username] = N'Jugador1')
BEGIN
    INSERT INTO [Usuarios] 
    ([Username], [Email], [PasswordHash], [PasswordSalt], [AvatarUrl], [ColorPerfil], [AvatarBorde], [AspectoJuego], [Monedas], [Nivel], [ExperienciaActual], [UltimaRecompensaDiaria])
    VALUES
    (N'Jugador1', N'jugador1@juegoclub.com', 0x00, 0x00, N'default_avatar.png', N'#3498db', N'default', N'default', 10000, 4, 250, NULL);
END;
GO


