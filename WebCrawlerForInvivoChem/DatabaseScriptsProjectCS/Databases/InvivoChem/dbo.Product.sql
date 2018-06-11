CREATE TABLE [dbo].[Product] (
    [Id]                       INT           IDENTITY (1, 1) NOT NULL,
    [CatalogNumber]            VARCHAR(10)   NOT NULL,
    [CASNumber]                VARCHAR(15)   NOT NULL,
    [Name]                     VARCHAR(32)   NOT NULL,
    [OtherName]                VARCHAR(32)   NULL,
    [ShortDescription]         VARCHAR (128) NULL,
	[LongDescription]          VARCHAR (MAX) NULL,
    [StockAmount]              INT           CONSTRAINT [DF_Product_StockAmount] DEFAULT ((0)) NOT NULL,
	[IsInStock]                BIT           CONSTRAINT [DF_Product_IsInStock] DEFAULT ((0)) NOT NULL,
	[ProductType]			   VARCHAR (32)  NULL,
    [Created]                  DATETIME      CONSTRAINT [DF_Product_Created] DEFAULT (getutcdate()) NOT NULL,
    [Modified]                 DATETIME      CONSTRAINT [DF_Product_Modified] DEFAULT (getutcdate()) NOT NULL,
    CONSTRAINT [PK_Product] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (FILLFACTOR = 90)
);


GO