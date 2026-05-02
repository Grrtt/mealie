using Mealie.Application.Contracts.Search;
using Mealie.Application.Services.ImageScrape;
using Mealie.Application.Services.Parser;
using Mealie.Infrastructure.Admin;
using NlpParserService = Mealie.Application.Services.IngredientParser.IngredientParserService;
using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Mealie.Infrastructure.Webhooks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mealie.Application.Queries;

public class QueryServices(
    ApplicationDbContext db,
    ITenantContext tenant,
    IMediator mediator,
    ILoggerFactory loggerFactory,
    IOptions<AppSettings> settings,
    NlpParserService ingredientParser,
    IIngredientParserService fullParser,
    ImageScrapeQueue imageScrapeQueue,
    IWebhookDeliveryService webhookDeliveryService,
    IRecipeSearchIndex searchIndex,
    IApiKeyEncryptionService encryptionService) : IQueryServices
{
    public ApplicationDbContext Db { get; } = db;
    public ITenantContext Tenant { get; } = tenant;
    public IMediator Mediator { get; } = mediator;
    public ILoggerFactory LoggerFactory { get; } = loggerFactory;
    public IOptions<AppSettings> Settings { get; } = settings;
    public NlpParserService IngredientParser { get; } = ingredientParser;
    public IIngredientParserService FullParser { get; } = fullParser;
    public ImageScrapeQueue ImageScrapeQueue { get; } = imageScrapeQueue;
    public IWebhookDeliveryService WebhookDeliveryService { get; } = webhookDeliveryService;
    public IRecipeSearchIndex SearchIndex { get; } = searchIndex;
    public IApiKeyEncryptionService EncryptionService { get; } = encryptionService;
}
