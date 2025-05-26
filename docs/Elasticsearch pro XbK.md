# Xperience by Kentico + Elasticsearch

Vyhledávání se dnes stává klíèovım prvkem témìø kadé webové aplikace. Xperience by Kentico nabízí nìkolik zpùsobù, jak efektivnì fulltextovì vyhledávat a filtrovat obsah spravovanı v CMS.

Mimo placené cloudové nástroje Azure AI search, Algolia a Recombee byla jedinou bezplatnou variantou integrace Lucene search hostovanı on-premise. Právì to nás motivovalo k implementaci integrace Elasticsearch jako alternativního nástroje umoòující on-premise hosting a je zcela zdarma pod licencí Elastic License 2.0. 

Elasticsearch je distribuovanı systém postavenı na technologii Apache Lucene. Vyniká vysokou škálovatelností, flexibilitou a širokou škálou moností pro zpracování rozsáhlıch dat a komplexních vyhledávacích poadavkù.

[comment]: <> (Vedle bezplatné varianty je navíc k dispozici i jako plnì spravovaná cloudová sluba, co jej èiní univerzálním øešením jak pro menší projekty, tak pro rozsáhlé enterprise aplikace.)

# Elasticsearch pro XbK 

Napojení Elasticsearch bìícího on-premise lze provést pomocí nìkolika jednoduchıch krokù. V tomto èlánku si ukáeme, jaké kroky je potøeba podniknout pro nastavení Elasticsearch, konfiguraci indexace a mapování dat z Kentico Xperience a následné vyhledávání.

## Instalace packages

Jako první je potøeba pøidat balíèek NuGet. V terminálu spuste následující pøíkaz

```
dotnet add package Kentico.Xperience.ElasticSearch
```


## Konfigurace Elasticsearch
Dále je potøeba pøidat následující konfiguraci do `appsettings.json` aplikace zahrnující Endpoint bìící instance Elasticsearch a údaje k autentizaci. Pro autentizaci lze vyuít buï pøihlašovací jméno a heslo, nebo API klíè, kterı lze vygenerovat v aplikaci Kibana.

```csharp
"CMSElasticSearch": {
 "SearchServiceEnabled": true,
 "SearchServiceEndPoint": "<your index application url>", //Endpoint bìící instance Elasticsearch
 "SearchServiceAPIKey": "<your API Key for Elasticsearch>"
 }
```

Lze alternativnì vyuít variantu s Username a heslem.
```csharp
"CMSElasticSearch": {
 ...
 "SearchServiceUsername": "<your index application username>",
 "SearchServicePassword": "<your index application password>",
 }
```

## Vytvoøení modelu a strategie
Hlavní funkcionalita této knihovny je postavena na konceptu vlastní indexaèní strategie, která se plnì pøizpùsobuje obsahovému modelu a poadovanému vyhledávacímu chování. Tato strategie umoòuje pøesnì urèit, jaká data se mají indexovat, jakım zpùsobem se mají mapovat do Elasticsearch a jak reagovat na zmìny v obsahu. V následujících krocích si ukáeme, jak si mùete tento proces nakonfigurovat pomocí pøipravenıch rozhraní a metod.


### Custom index model
Definujte vlastní model vyhledávání rozšíøením modelu `BaseElasticSearchModel` poskytovaného knihovnou, kterı bude pouit k vytvoøení indexu v Elasticsearch.

```csharp
public class DancingGoatSearchModel : BaseElasticSearchModel
{
    public string Title { get; set; }
    
    public string Content { get; set; }
}
```

### Implementace Indexing Strategy
Definujte vlastní implementaci `BaseElasticSearchIndexingStrategy<TSearchModel>`, abyste mohli pøizpùsobit zpùsob, jakım jsou web page items nebo content items zpracovávány pro indexování.

```csharp
public class DancingGoatSearchStrategy(...) : BaseElasticSearchIndexingStrategy<DancingGoatSearchModel>
{
    ...
}
```

#### Nastavení polí (TypeMapping)
Dále je potøeba urèit, jak budou jednotlivá pole modelu uloena v indexu Elasticsearch. Vytvoøte override metodu `Mapping(TypeMappingDescriptor<TSearchModel> descriptor)`. Tato metoda umoòuje definovat datové typy polí a nastavit jejich chování v rámci vyhledávání – napøíklad zda budou slouit k fulltextovému vyhledávání (text) nebo k pøesnému filtrování (keyword).


```csharp
public override void Mapping(TypeMappingDescriptor<DancingGoatSearchModel> descriptor) =>
    descriptor
        .Properties(props => props
            .Keyword(x => x.Title)
            .Text(x => x.Content));
```

Celı seznam typù najdete v oficiální dokumentaci Elasticsearch https://www.elastic.co/docs/reference/elasticsearch/mapping-reference/field-data-types.

#### Mapování obsahu na search model
Dalším krokem je definice mapování jednotlivıch vlastností (properties) obsahu do našeho vlastního index modelu. 

Pøepište metodu `Task<IElasticSearchModel?> MapToElasticSearchModelOrNull(IIndexEventItemModel item)` a definujte mapování na vlastní implementaci `BaseElasticSearchModel` (v této ukázce tedy `DancingGoatSearchModel`). Spoleèné vlastnosti definované v base class `BaseElasticSearchModel` jsou mapovány automaticky. Je nutné tedy mapovat pouze custom pole daného content typu.

V následující code snippet ukázce je znázornìno mapování typu `ArticlePage` s vlastností ArticleTitle a raw contentem obsahu stránky na `DancingGoatSearchModel`.

```csharp
public override async Task<IElasticSearchModel?> MapToElasticSearchModelOrNull(IIndexEventItemModel item)
{
    var result = new DancingGoatSearchModel();

    if (item is not IndexEventWebPageItemModel indexedPage)
    {
        return null;
    }

    if (string.Equals(item.ContentTypeName, ArticlePage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
    {
        var page = await strategyHelper.GetPage<ArticlePage>(
            indexedPage.ItemGuid,
            indexedPage.WebsiteChannelName,
            indexedPage.LanguageName,
            ArticlePage.CONTENT_TYPE_NAME);

        if (page is null)
        {
            return null;
        }

        result.Title = page.ArticleTitle ?? string.Empty;
        var rawContent = await webCrawler.CrawlWebPage(page!);
        result.Content = htmlSanitizer.SanitizeHtmlDocument(rawContent);
    }

    return result;
}
```

> [!NOTE]  
> `IIndexEventItemModel` je abstraktní tøída poloky zpracovávané pro indexování. Zahrnuje `IndexEventWebPageItemModel` pro poloky webovıch stránek, tak `IndexEventReusableItemModel` pro poloky opakovanì pouitelného obsahu.


Záleí na konkrétní implementaci, jakım zpùsobem se naèítají obsahová data urèená k indexaci. Lze napøíklad vyuít generickou metodu GetPage<T>, jak je ukázáno v tomto pøíkladu:
https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch/-/blob/IN-654-Elastic-search-from-azure/docs/Custom-index-strategy.md?ref_type=heads#data-retrieval-during-indexing.   


#### Aktualizace navázaného obsahu v indexu
Pøímá manipulace s konkrétní polokou v CMS automaticky spouští navázané události (eventy), které zajistí, e odpovídající záznam v indexu zùstane aktuální.
Co se ale stane, pokud dojde ke zmìnì navázaného obsahu, napøíklad opakovanì pouitelného prvku (reusable content item), kterı je souèástí více stránek?

V takovém pøípadì je potøeba implementovat logiku, která na základì zmìny v navázaném obsahu vyhodnotí, které další poloky v indexu je nutné pøeindexovat. K tomu slouí metoda `FindItemsToReindex`. Všechny poloky vrácené z této metody budou pøedány do `MapToElasticSearchModelOrNull(IIndexEventItemModel item)` pro indexaci.

Ukázka implementace této metody:
https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch/-/blob/IN-654-Elastic-search-from-azure/docs/Custom-index-strategy.md?ref_type=heads#keeping-indexed-related-content-up-to-date




#### Registrace Strategie
Aby bylo moné vlastní strategii pouít, je potøeba ji zaregistrovat pomocí dependency injection (DI):
```csharp
services.AddKenticoElasticSearch(builder =>
{
    builder.RegisterStrategy<DancingGoatSearchStrategy, DancingGoatSearchModel>(nameof(DancingGoatSearchStrategy));
}, configuration);
```



## Nastavení indexu v administraci XbyK
Dalším krokem je vytvoøení samotného indexu v administraci Xperience. To provedete v aplikaci Elastic Search, kterou do systému pøidává tato knihovna. Zde nastavíte název indexu, vyberete odpovídající strategii, jazykové varianty, kanály a typy obsahu, které se mají indexovat.

![XbyK create index](/images/xperience-administration-search-index-edit-form.png)

Po vytvoøení a nakonfigurování indexu je potøeba provést jeho pøegenerování, spuštìním akce Rebuild na stránce List of registered Elastic Search indices.

![XbyK rebuild index](/images/xperience-administration-search-index-list.png)


Po této akci by ji mìl bıt index naplnìnı polokami (dle implementace `DancingGoatSearchStrategy`) a pøipravenı k samotnému vyhledávání a filtrování.


Odkaz na dokumentaci: https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch/-/blob/IN-654-Elastic-search-from-azure/docs/Managing-Indexes.md



## Vyhledávání
Závìreènım krokem je ji zbıvá samotná implementace vyhledávání. 

Proveïte vyhledávání s vlastním nastavením "search options" pomocí sluby `IElasticSearchQueryClientService`. Urèete parametry vyhledávání a vyberte data, která budou získána z Elasticsearch indexu.

```csharp
    var index = searchClientService.CreateSearchClientForQueries(indexName);

    page = Math.Max(page, 1);
    pageSize = Math.Max(1, pageSize);

    var request = new SearchRequest(indexName)
    {
        From = (page - 1) * pageSize,
        Size = pageSize,
        Query = string.IsNullOrEmpty(searchText)
            ? new MatchAllQuery()
            : new MultiMatchQuery()
            {
                Fields = new[]
                {
                    nameof(DancingGoatSearchModel.Title).ToLower(),
                },
                Query = searchText,
            },
        TrackTotalHits = new TrackHits(true)
    };

    var response = await index.SearchAsync<DancingGoatSearchModel>(request);
```

Pøi implementaci vyhledávání se vyuívá standardní ElasticsearchClient (.NET Client v8) s moností vyuít Fluent API nebo Object initializer API. Rozdíl mezi tìmito pøístupy lze vidìt zde https://www.elastic.co/docs/reference/elasticsearch/clients/dotnet/query



## Závìr

Integrace Elasticsearch do Xperience by Kentico rozšiøuje monosti vyhledávání a nabízí flexibilitu, kterou u jinıch fulltextovıch nástrojù nenajdeme. Tato integrace umoòuje rychlou indexaci, pokroèilé dotazování a monost hostování on-premise, co je velká vıhoda pro uivatele, kteøí chtìjí mít plnou kontrolu nad svımi daty. S dalším rozvojem technologie se dá oèekávat, e se Elasticsearch stane ještì atraktivnìjším øešením pro vyhledávací poadavky v Xperience by Kentico.

Doufáme, e vám tento èlánek pomohl lépe se zorientovat v integraci Elasticsearch do Xperience by Kentico. Monosti této integrace ale sahají ještì dál, ne bylo uvedeno v tomto èlánku. V dokumentaci integrace najdete napøíklad i ukázku pro inspiraci pøi [crawlování stránek](https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch/-/blob/IN-654-Elastic-search-from-azure/docs/Scraping-web-page-content.md?ref_type=heads#scraping-web-page-content) nebo pøi správì indexovıch [aliasù](https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch/-/blob/IN-654-Elastic-search-from-azure/docs/Managing-Aliases.md?ref_type=heads#managing-aliases).


Detailnìjší návod k vytvoøení vlastní indexaèní strategie (vèetnì code snippets), zpùsobu mapování dat a propojení s Kentico Xperience, naleznete pøímo v oficiálním repozitáøi knihovny. Odkaz na GitLab repozitáø: https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch.
