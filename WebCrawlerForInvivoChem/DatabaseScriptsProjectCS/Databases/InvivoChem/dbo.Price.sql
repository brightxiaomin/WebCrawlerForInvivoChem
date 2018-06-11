CREATE TABLE [dbo].[Price] (
    [Id]                       INT           IDENTITY (1, 1) NOT NULL,
    [CASNumber]                VARCHAR(15)   NOT NULL,
	[Price]                    MONEY         NOT NULL,
	[Amount]                   INT           NOT NULL,
    [Created]                  DATETIME      CONSTRAINT [DF_Price_Created] DEFAULT (getutcdate()) NOT NULL,
    [Modified]                 DATETIME      CONSTRAINT [DF_Price_Modified] DEFAULT (getutcdate()) NOT NULL,
    CONSTRAINT [PK_Price] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (FILLFACTOR = 90)
);


GO