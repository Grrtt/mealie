using Mealie.Application.Contracts.Search;
using Mealie.Application.Services.ImageScrape;
using Mealie.Application.Services.IngredientParser;
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
    IngredientParserService ingredientParser,
    ImageScrapeQueue imageScrapeQueue,
    IWebhookDeliveryService webhookDeliveryService,
    IRecipeSearchIndex searchIndex) : IQueryServices
{
    public ApplicationDbContext Db { get; } = db;
    public ITenantContext Tenant { get; } = tenant;
    public IMediator Mediator { get; } = mediator;
    public ILoggerFactory LoggerFactory { get; } = loggerFactory;
    public IOptions<AppSettings> Settings { get; } = settings;
    public IngredientParserService IngredientParser { get; } = ingredientParser;
    public ImageScrapeQueue ImageScrapeQueue { get; } = imageScrapeQueue;
    public IWebhookDeliveryService WebhookDeliveryService { get; } = webhookDeliveryService;
    public IRecipeSearchIndex SearchIndex { get; } = searchIndex;
}
