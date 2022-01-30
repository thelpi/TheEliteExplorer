USE [master]
GO
/****** Object:  Database [elite]    Script Date: 30/01/2022 18:59:05 ******/
CREATE DATABASE [elite]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'elite', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL12.SQLEXPRESS\MSSQL\DATA\elite.mdf' , SIZE = 1410048KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
 LOG ON 
( NAME = N'elite_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL12.SQLEXPRESS\MSSQL\DATA\elite_log.ldf' , SIZE = 7460992KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO
ALTER DATABASE [elite] SET COMPATIBILITY_LEVEL = 120
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [elite].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [elite] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [elite] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [elite] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [elite] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [elite] SET ARITHABORT OFF 
GO
ALTER DATABASE [elite] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [elite] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [elite] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [elite] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [elite] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [elite] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [elite] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [elite] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [elite] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [elite] SET  DISABLE_BROKER 
GO
ALTER DATABASE [elite] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [elite] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [elite] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [elite] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [elite] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [elite] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [elite] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [elite] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [elite] SET  MULTI_USER 
GO
ALTER DATABASE [elite] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [elite] SET DB_CHAINING OFF 
GO
ALTER DATABASE [elite] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [elite] SET TARGET_RECOVERY_TIME = 0 SECONDS 
GO
ALTER DATABASE [elite] SET DELAYED_DURABILITY = DISABLED 
GO
USE [elite]
GO
/****** Object:  User [MyAppPoolUser2]    Script Date: 30/01/2022 18:59:05 ******/
CREATE USER [MyAppPoolUser2] FOR LOGIN [IIS APPPOOL\TheEliteApi] WITH DEFAULT_SCHEMA=[dbo]
GO
/****** Object:  User [MyAppPoolUser]    Script Date: 30/01/2022 18:59:05 ******/
CREATE USER [MyAppPoolUser] FOR LOGIN [IIS APPPOOL\TheElite] WITH DEFAULT_SCHEMA=[dbo]
GO
ALTER ROLE [db_owner] ADD MEMBER [MyAppPoolUser2]
GO
ALTER ROLE [db_datareader] ADD MEMBER [MyAppPoolUser2]
GO
ALTER ROLE [db_datawriter] ADD MEMBER [MyAppPoolUser2]
GO
ALTER ROLE [db_owner] ADD MEMBER [MyAppPoolUser]
GO
ALTER ROLE [db_datareader] ADD MEMBER [MyAppPoolUser]
GO
ALTER ROLE [db_datawriter] ADD MEMBER [MyAppPoolUser]
GO
/****** Object:  UserDefinedFunction [dbo].[get_estimated_date]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[get_estimated_date]
(
	@player_id BIGINT, @stage_id BIGINT, @level_id BIGINT, @time BIGINT, @system_id BIGINT, @rule_id BIGINT
)
RETURNS DATETIME2(7)
AS
BEGIN
	
	DECLARE @final_date DATETIME2(7)
	DECLARE @same_time_entry DATETIME2(7)

	/* same entry, with a date, on a different system */
	SET @same_time_entry = ISNULL((
		SELECT TOP 1 [e].[date]
		FROM [dbo].[entry] AS [e] WITH(NOLOCK)
		where [e].player_id = @player_id
			AND [e].[date] IS NOT NULL
			AND [e].[stage_id] = @stage_id
			AND [e].[level_id] = @level_id
			AND [e].[time] = @time
	), NULL)

	IF @same_time_entry IS NOT NULL
		SET @final_date = @same_time_entry
	ELSE
		BEGIN

			DECLARE @real_min_borne DATETIME2(7)
			DECLARE @real_max_borne DATETIME2(7)
			DECLARE @better_time_entry DATETIME2(7)
			DECLARE @worse_time_entry DATETIME2(7)

			DECLARE @end_empty_date DATETIME2(7) = '2013-01-01'
			
			/* closest time for this stage/level; upper bound */
			SET @better_time_entry = ISNULL((
				SELECT TOP 1 [e].[date]
				FROM [dbo].[entry] AS [e] WITH(NOLOCK)
				where [e].player_id = @player_id
					AND [e].[date] IS NOT NULL
					AND [e].[date] < @end_empty_date
					AND [e].[stage_id] = @stage_id
					AND [e].[level_id] = @level_id
					AND [e].[time] < @time
				ORDER BY [e].[time] DESC
			), NULL)

			/* closest time for this stage/level; lower bound */
			SET @worse_time_entry = ISNULL((
				SELECT TOP 1 [e].[date]
				FROM [dbo].[entry] AS [e] WITH(NOLOCK)
				where [e].player_id = @player_id
					AND [e].[date] IS NOT NULL
					AND [e].[stage_id] = @stage_id
					AND [e].[level_id] = @level_id
					AND [e].[time] > @time
				ORDER BY [e].[time]
			), NULL)

			/* IFNULL: oldest date player's entries, or date of the begining of the game in the worst case */
			SET @real_min_borne = ISNULL(@worse_time_entry, (
				SELECT TOP 1 ISNULL([p].join_date, (
					SELECT [g].[begin_date]
					FROM [dbo].[game] AS [g]
						INNER JOIN [dbo].[stage] AS [s] WITH(NOLOCK) ON [g].[id] = [s].[game_id]
					WHERE [s].[id] = @stage_id
				))
				FROM [dbo].[player] AS [p] WITH(NOLOCK)
				WHERE [p].[id] = @player_id
			))
			
			/* IFNULL: newest date player's entries, or max date allowed overall in the worst case */
			SET @real_max_borne = ISNULL(@better_time_entry, ISNULL((
					SELECT TOP 1 [e].[date]
					FROM [dbo].[entry] AS [e] WITH(NOLOCK)
					where [e].player_id = @player_id
						AND [e].[date] IS NOT NULL
					ORDER BY [e].[date] DESC
				), @end_empty_date
			))

			IF (@rule_id = 1)
				SET @final_date = @real_min_borne
			ELSE IF (@rule_id = 2)
				SET @final_date = @real_max_borne
			ELSE IF (@rule_id = 3)
				SET @final_date = DATEADD(dd, DATEDIFF(dd, @real_min_borne , @real_max_borne) / 2, @real_min_borne)
			ELSE IF (@rule_id = 4)
				SET @final_date = DATEADD(dd, DATEDIFF(dd, @real_min_borne , @real_max_borne) / 2, @real_min_borne)
		END

	RETURN @final_date

END

GO
/****** Object:  Table [dbo].[calendar]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[calendar](
	[date] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_calendar] PRIMARY KEY CLUSTERED 
(
	[date] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[entry]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[entry](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[player_id] [bigint] NOT NULL,
	[level_id] [bigint] NOT NULL,
	[stage_id] [bigint] NOT NULL,
	[date] [datetime2](7) NULL,
	[time] [bigint] NOT NULL,
	[system_id] [bigint] NULL,
 CONSTRAINT [PK_entry] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[entry_dirty]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[entry_dirty](
	[id] [bigint] NOT NULL,
	[player_id] [bigint] NOT NULL,
	[level_id] [bigint] NOT NULL,
	[stage_id] [bigint] NOT NULL,
	[date] [datetime2](7) NULL,
	[time] [bigint] NOT NULL,
	[system_id] [bigint] NULL
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[game]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[game](
	[id] [bigint] NOT NULL,
	[name] [nvarchar](255) NOT NULL,
	[begin_date] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_game] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[level]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[level](
	[id] [bigint] NOT NULL,
	[name] [nvarchar](255) NOT NULL,
 CONSTRAINT [PK_level] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[player]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[player](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[url_name] [nvarchar](255) NOT NULL,
	[real_name] [nvarchar](255) NOT NULL,
	[surname] [nvarchar](255) NOT NULL,
	[color] [char](6) NOT NULL,
	[control_style] [varchar](10) NULL,
	[is_dirty] [bit] NOT NULL,
	[join_date] [datetime2](7) NULL,
	[is_banned] [bit] NOT NULL,
 CONSTRAINT [PK_player] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[stage]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stage](
	[id] [bigint] NOT NULL,
	[name] [nvarchar](255) NOT NULL,
	[game_id] [bigint] NOT NULL,
 CONSTRAINT [PK_stage] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Index [IX_date]    Script Date: 30/01/2022 18:59:05 ******/
CREATE NONCLUSTERED INDEX [IX_date] ON [dbo].[entry]
(
	[date] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_level_id]    Script Date: 30/01/2022 18:59:05 ******/
CREATE NONCLUSTERED INDEX [IX_level_id] ON [dbo].[entry]
(
	[level_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_player_id]    Script Date: 30/01/2022 18:59:05 ******/
CREATE NONCLUSTERED INDEX [IX_player_id] ON [dbo].[entry]
(
	[player_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_stage_id]    Script Date: 30/01/2022 18:59:05 ******/
CREATE NONCLUSTERED INDEX [IX_stage_id] ON [dbo].[entry]
(
	[stage_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_stage_id_level_id]    Script Date: 30/01/2022 18:59:05 ******/
CREATE NONCLUSTERED INDEX [IX_stage_id_level_id] ON [dbo].[entry]
(
	[level_id] ASC,
	[stage_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_stage_id_level_id_player_id_time]    Script Date: 30/01/2022 18:59:05 ******/
CREATE NONCLUSTERED INDEX [IX_stage_id_level_id_player_id_time] ON [dbo].[entry]
(
	[player_id] ASC,
	[level_id] ASC,
	[stage_id] ASC,
	[time] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_system_id]    Script Date: 30/01/2022 18:59:05 ******/
CREATE NONCLUSTERED INDEX [IX_system_id] ON [dbo].[entry]
(
	[system_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_time]    Script Date: 30/01/2022 18:59:05 ******/
CREATE NONCLUSTERED INDEX [IX_time] ON [dbo].[entry]
(
	[time] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
ALTER TABLE [dbo].[player] ADD  CONSTRAINT [DF_player_is_dirty]  DEFAULT ((0)) FOR [is_dirty]
GO
ALTER TABLE [dbo].[player] ADD  CONSTRAINT [DF_player_is_banned]  DEFAULT ((0)) FOR [is_banned]
GO
ALTER TABLE [dbo].[entry]  WITH CHECK ADD  CONSTRAINT [FK_entry_player] FOREIGN KEY([player_id])
REFERENCES [dbo].[player] ([id])
GO
ALTER TABLE [dbo].[entry] CHECK CONSTRAINT [FK_entry_player]
GO
ALTER TABLE [dbo].[stage]  WITH CHECK ADD  CONSTRAINT [FK_stage_game] FOREIGN KEY([game_id])
REFERENCES [dbo].[game] ([id])
GO
ALTER TABLE [dbo].[stage] CHECK CONSTRAINT [FK_stage_game]
GO
/****** Object:  StoredProcedure [dbo].[delete_entry]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[delete_entry]
	@stage_id BIGINT,
	@level_id BIGINT
AS
BEGIN

	DELETE FROM [dbo].[entry]
	WHERE stage_id = @stage_id
		AND level_id = @level_id

END


GO
/****** Object:  StoredProcedure [dbo].[delete_player]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[delete_player]
	@id BIGINT
AS
BEGIN

	DELETE FROM [dbo].[player]
	WHERE [id] = @id

END


GO
/****** Object:  StoredProcedure [dbo].[delete_player_entry]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[delete_player_entry]
	@stage_id BIGINT,
	@player_id BIGINT
AS
BEGIN

	DELETE FROM [dbo].[entry]
	WHERE player_id = @player_id
		AND stage_id = @stage_id

END


GO
/****** Object:  StoredProcedure [dbo].[insert_entry]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[insert_entry]
	@player_id BIGINT
	,@level_id BIGINT
	,@stage_id BIGINT
	,@date DATETIME2
	,@time BIGINT
	,@system_id BIGINT
	,@id BIGINT OUTPUT
AS
BEGIN
	
	INSERT INTO [dbo].[entry]
		([player_id],[level_id],[stage_id],[date],[time],[system_id])
	VALUES
		(@player_id,@level_id,@stage_id,@date,@time,@system_id)

	SET @id = SCOPE_IDENTITY()
END


GO
/****** Object:  StoredProcedure [dbo].[insert_player]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[insert_player]
	@url_name NVARCHAR(255)
	, @real_name NVARCHAR(255)
	, @surname NVARCHAR(255)
	, @color CHAR(6)
	, @is_dirty BIT
	, @control_style VARCHAR(10)
	, @join_date DATETIME2
	, @id BIGINT OUTPUT
AS
BEGIN

	INSERT INTO [dbo].[player]
		([url_name], [real_name], [surname], [color], [control_style], [is_dirty], [join_date])
	VALUES
		(@url_name, @real_name, @surname, @color, @control_style, @is_dirty, @join_date)

	SET @id = SCOPE_IDENTITY()

END


GO
/****** Object:  StoredProcedure [dbo].[select_all_entry]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[select_all_entry]
	@stage_id BIGINT
AS
BEGIN
	
	SET NOCOUNT ON

	SELECT [entry].[id]
		, [entry].[date]
		, [entry].[level_id] AS [Level]
		, [entry].[player_id]
		, [entry].[stage_id] AS [Stage]
		, [entry].[system_id] AS [Engine]
		, [entry].[time]
	FROM [dbo].[entry] WITH(NOLOCK)
	WHERE [stage_id] = @stage_id

END


GO
/****** Object:  StoredProcedure [dbo].[select_duplicate_players]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[select_duplicate_players]
AS
BEGIN

	SET NOCOUNT ON

	SELECT [p].[id]
		,[p].[url_name]
	FROM [dbo].[player] AS [p] WITH(NOLOCK)
	WHERE EXISTS (
		SELECT 1 FROM [dbo].[player] AS [p2] WITH(NOLOCK)
		WHERE [p2].[url_name] = [p].[url_name]
			AND [p2].[id] != [p].[id]
	)
	ORDER BY [p].[url_name] ASC
		, [p].[is_dirty] ASC
		, (
			SELECT COUNT(*)
			FROM [dbo].[entry] WITH(NOLOCK)
			WHERE [player_id] = [p].[id]
		) DESC

END


GO
/****** Object:  StoredProcedure [dbo].[select_entry]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[select_entry]
	@stage_id BIGINT,
	@level_id BIGINT,
	@start_date DATETIME2,
	@end_date DATETIME2
AS
BEGIN
	
	SET NOCOUNT ON

	SELECT [id]
		, [date]
		, [level_id] AS [Level]
		, [player_id]
		, [stage_id] AS [Stage]
		, [system_id] AS [Engine]
		, [time]
	FROM [dbo].[entry] WITH(NOLOCK)
	WHERE [stage_id] = @stage_id
		AND [level_id] = @level_id
		AND (
			@start_date IS NULL
			OR [date] >= @start_date
		)
		AND (
			@end_date IS NULL
			OR [date] < @end_date
		)

END


GO
/****** Object:  StoredProcedure [dbo].[select_entry_by_entry]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[select_entry_by_entry]
	@entry_id BIGINT
AS
BEGIN
	
	SET NOCOUNT ON

	SELECT [id]
		, [date]
		, [level_id] AS [Level]
		, [player_id]
		, [stage_id] AS [Stage]
		, [system_id] AS [Engine]
		, [time]
	FROM [dbo].[entry] AS [e] WITH(NOLOCK)
	WHERE [id] <> @entry_id
		AND EXISTS (
			SELECT 1 FROM [dbo].[entry] AS [e2] WITH(NOLOCK)
			WHERE [e].[time] = [e2].[time]
				AND [e].[level_id] = [e2].[level_id]
				AND [e].[stage_id] = [e2].[stage_id]
				AND ISNULL([e].[system_id], -1) = ISNULL([e2].[system_id], -1)
				AND [e].[date] = [e2].[date]
				AND [e2].[id] = @entry_id
		)

END


GO
/****** Object:  StoredProcedure [dbo].[select_entry_count]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[select_entry_count]
	@stage_id BIGINT,
	@level_id BIGINT,
	@start_date DATETIME2,
	@end_date DATETIME2
AS
BEGIN
	
	SET NOCOUNT ON

	SELECT COUNT(*)
	FROM [dbo].[entry] WITH(NOLOCK)
	WHERE [stage_id] = @stage_id
		AND (@level_id IS NULL OR [level_id] = @level_id)
		AND (
			@start_date IS NULL
			OR [date] >= @start_date
		)
		AND (
			@end_date IS NULL
			OR [date] < @end_date
		)

END


GO
/****** Object:  StoredProcedure [dbo].[select_latest_entry_date]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[select_latest_entry_date]
AS
BEGIN
	
	SET NOCOUNT ON

	SELECT MAX([date])
	FROM [dbo].[entry] WITH(NOLOCK)
	WHERE [date] IS NOT NULL

END


GO
/****** Object:  StoredProcedure [dbo].[select_player]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[select_player]
	@is_dirty BIT
AS
BEGIN

	SET NOCOUNT ON

	SELECT [id]
		,[url_name]
		,[real_name]
		,[surname]
		,[color]
		,[control_style]
		,[is_dirty]
		,[join_date]
	FROM [dbo].[player] WITH(NOLOCK)
	WHERE [is_dirty] = @is_dirty
		AND [is_banned] = 0

END


GO
/****** Object:  StoredProcedure [dbo].[select_player_entry]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[select_player_entry]
	@player_id BIGINT
AS
BEGIN
	
	SET NOCOUNT ON

	SELECT [id]
		, [date]
		, [level_id] AS [Level]
		, [player_id]
		, [stage_id] AS [Stage]
		, [system_id] AS [Engine]
		, [time]
	FROM [dbo].[entry] WITH(NOLOCK)
	WHERE [player_id] = @player_id

END


GO
/****** Object:  StoredProcedure [dbo].[select_stage_level_ranking]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[select_stage_level_ranking]
	@stage_id BIGINT,
	@level_id BIGINT,
	@date DATETIME2(7),
	@rule_id BIGINT
AS
BEGIN

	SET NOCOUNT ON;

    SELECT  [p].[id] as [player_id]
		, [p].[real_name]
		, [p].[surname]
		, MIN([r].[time]) as [time]
		, (
			SELECT TOP 1 ISNULL([alt].[date], [dbo].[get_estimated_date]([alt].[player_id], [alt].[stage_id], [alt].[level_id], [alt].[time], [alt].[system_id], @rule_id))
			FROM [dbo].[entry] AS [alt] WITH(NOLOCK)
			WHERE [alt].[stage_id] = @stage_id
				AND [alt].[level_id] = @level_id
				AND [alt].[player_id] = [p].[id]
				AND [alt].[time] = MIN([r].[time])
			ORDER BY ISNULL([alt].[date], [dbo].[get_estimated_date]([alt].[player_id], [alt].[stage_id], [alt].[level_id], [alt].[time], [alt].[system_id], @rule_id)) ASC
		) AS [date]
	FROM [dbo].[entry] AS [r] WITH(NOLOCK)
		INNER JOIN [dbo].[player] AS [p] WITH(NOLOCK) ON [r].[player_id] = [p].[id]
	WHERE [r].[stage_id] = @stage_id
		AND [r].[level_id] = @level_id
		AND ISNULL([r].[date], [dbo].[get_estimated_date]([r].[player_id], [r].[stage_id], [r].[level_id], [r].[time], [r].[system_id], @rule_id)) <= ISNULL(@date, GETDATE())
	GROUP BY [p].[id], [p].[real_name], [p].[surname]
	ORDER BY [time], [date]

END

GO
/****** Object:  StoredProcedure [dbo].[update_dirty_player]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[update_dirty_player]
	@id BIGINT
AS
BEGIN

	UPDATE [dbo].[player] WITH(ROWLOCK)
	SET [is_dirty] = 1
	WHERE [id] = @id

END


GO
/****** Object:  StoredProcedure [dbo].[update_entry_date]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[update_entry_date]
	@entry_id BIGINT,
	@date DATETIME2(7)
AS
BEGIN

	UPDATE [dbo].[entry] WITH(ROWLOCK)
	SET [date] = @date
	WHERE [id] = @entry_id

END

GO
/****** Object:  StoredProcedure [dbo].[update_entry_player]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[update_entry_player]
	@current_player_id BIGINT
	, @new_player_id BIGINT
AS
BEGIN

	UPDATE [dbo].[entry] WITH(ROWLOCK)
	SET [player_id] = @new_player_id
	WHERE [player_id] = @current_player_id

END


GO
/****** Object:  StoredProcedure [dbo].[update_player]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[update_player]
	@id BIGINT
	, @real_name NVARCHAR(255)
	, @surname NVARCHAR(255)
	, @color CHAR(6)
	, @control_style VARCHAR(10)
AS
BEGIN

	UPDATE [dbo].[player] WITH(ROWLOCK)
	SET [real_name] = @real_name
		, [surname] = @surname
		, [color] = @color
		, [control_style] = @control_style
		, [is_dirty] = 0
	WHERE [id] = @id

END


GO
/****** Object:  StoredProcedure [dbo].[update_player_join_date]    Script Date: 30/01/2022 18:59:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[update_player_join_date]
AS
BEGIN
	
	UPDATE [dbo].[player] WITh(ROWLOCK)
	SET [join_date] = (
		SELECT TOP 1 [date]
		FROM [dbo].[entry] WITH(NOLOCK)
		WHERE [date] IS NOT NULL
			AND [player_id] = [player].[id]
		ORDER BY [date] ASC
	)

END

GO
USE [master]
GO
ALTER DATABASE [elite] SET  READ_WRITE 
GO
