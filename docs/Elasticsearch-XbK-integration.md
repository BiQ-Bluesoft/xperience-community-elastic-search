# Xperience by Kentico + Elasticsearch

Vyhledávání se dnes stává klíčovým prvkem téměř každé webové aplikace. Xperience by Kentico nabízí několik způsobů, jak efektivně fulltextově vyhledávat a filtrovat obsah spravovaný v CMS.

Kromě placených cloudových nástrojů, jako jsou Azure AI Search, Algolia nebo Recombee, byla dosud jedinou bezplatnou on-premise variantou integrace vyhledávání pomocí Lucene. Právě to nás motivovalo k implementaci integrace Elasticsearch jako alternativního nástroje umožňující on-premise hosting. Elasticsearch je zcela zdarma pod licencí Elastic License 2.0. 

Elasticsearch je distribuovaný systém postavený na technologii Apache Lucene. Vyniká vysokou škálovatelností, flexibilitou a širokou škálou možností pro zpracování rozsáhlých dat a komplexních vyhledávacích požadavků.

[comment]: <> (Vedle bezplatné varianty je navíc k dispozici i jako plně spravovaná cloudová služba, což jej činí univerzálním řešením jak pro menší projekty, tak pro rozsáhlé enterprise aplikace.)

# Elasticsearch pro XbK 

Napojení Elasticsearch běžícího on-premise lze provést pomocí několika jednoduchých kroků. V tomto článku si ukážeme, jaké kroky je potřeba podniknout pro nastavení Elasticsearch, konfiguraci indexace a mapování dat z Kentico Xperience a následné vyhledávání.

## 1. Instalace packages

Jako první je potřeba přidat balíček NuGet. V terminálu spusťte následující příkaz

```
dotnet add package Kentico.Xperience.ElasticSearch
```


## 2. Konfigurace Elasticsearch
Dále je potřeba přidat následující konfiguraci do `appsettings.json` aplikace zahrnující Endpoint běžící instance Elasticsearch a údaje k autentizaci. Pro autentizaci lze využít buď přihlašovací jméno a heslo, nebo API klíč, který lze vygenerovat v aplikaci Kibana.

```json
"CMSElasticSearch": {
 "SearchServiceEnabled": true,
 "SearchServiceEndPoint": "<your index application url>", //Endpoint běžící instance Elasticsearch
 "SearchServiceAPIKey": "<your API Key for Elasticsearch>"
 }
```

Lze alternativně využít variantu s Username a heslem.
```json
"CMSElasticSearch": {
 // ...
 // ...
 "SearchServiceUsername": "<your index application username>",
 "SearchServicePassword": "<your index application password>",
 }
```

## 3. Vytvoření modelu a strategie
Hlavní funkcionalita této knihovny je postavena na konceptu vlastní indexační strategie, která se plně přizpůsobuje obsahovému modelu a požadovanému vyhledávacímu chování. Tato strategie umožňuje přesně určit, jaká data se mají indexovat, jakým způsobem se mají mapovat do Elasticsearch a jak reagovat na změny v obsahu. V následujících krocích si ukážeme, jak si můžete tento proces nakonfigurovat pomocí připravených rozhraní a metod.


### Custom index model
Definujte vlastní model vyhledávání rozšířením modelu `BaseElasticSearchModel` poskytovaného knihovnou, který bude použit k vytvoření indexu v Elasticsearch.

```csharp
public class DancingGoatSearchModel : BaseElasticSearchModel
{
    public string Title { get; set; }
    
    public string Content { get; set; }
}
```

### Implementace Indexing Strategy
Definujte vlastní implementaci `BaseElasticSearchIndexingStrategy<TSearchModel>`, abyste mohli přizpůsobit způsob, jakým jsou web page items nebo content items zpracovávány pro indexování.

```csharp
public class DancingGoatSearchStrategy(...) : BaseElasticSearchIndexingStrategy<DancingGoatSearchModel>
{
    ...
}
```

#### Nastavení polí (TypeMapping)
Dále je potřeba určit, jak budou jednotlivá pole modelu uložena v indexu Elasticsearch. Vytvořte override metodu `Mapping(TypeMappingDescriptor<TSearchModel> descriptor)`. Tato metoda umožňuje definovat datové typy polí a nastavit jejich chování v rámci vyhledávání – například zda budou sloužit k fulltextovému vyhledávání (text) nebo k přesnému filtrování (keyword).


```csharp
public override void Mapping(TypeMappingDescriptor<DancingGoatSearchModel> descriptor) =>
    descriptor
        .Properties(props => props
            .Keyword(x => x.Title)
            .Text(x => x.Content));
```

Celý seznam typů najdete v oficiální dokumentaci Elasticsearch https://www.elastic.co/docs/reference/elasticsearch/mapping-reference/field-data-types.

#### Mapování obsahu na search model
Dalším krokem je definice mapování jednotlivých vlastností (properties) obsahu do našeho vlastního index modelu. 

Přepište metodu `Task<IElasticSearchModel?> MapToElasticSearchModelOrNull(IIndexEventItemModel item)` a definujte mapování na vlastní implementaci `BaseElasticSearchModel` (v této ukázce tedy `DancingGoatSearchModel`). Společné vlastnosti definované v base class `BaseElasticSearchModel` jsou mapovány automaticky. Je nutné tedy mapovat pouze custom pole daného content typu.

V následující code snippet ukázce je znázorněno mapování typu `ArticlePage` s vlastností ArticleTitle a raw contentem obsahu stránky na `DancingGoatSearchModel`.

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

`IIndexEventItemModel` je abstraktní třída položky zpracovávané pro indexování. Zahrnuje `IndexEventWebPageItemModel` pro položky webových stránek, tak `IndexEventReusableItemModel` pro položky opakovaně použitelného obsahu.


Záleží na konkrétní implementaci, jakým způsobem se načítají obsahová data určená k indexaci. Lze například využít generickou metodu GetPage<T>, jak je ukázáno v tomto příkladu:
https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch/-/blob/IN-654-Elastic-search-from-azure/docs/Custom-index-strategy.md?ref_type=heads#data-retrieval-during-indexing.   


#### Aktualizace navázaného obsahu v indexu
Přímá manipulace s konkrétní položkou v CMS automaticky spouští navázané události (eventy), které zajistí, že odpovídající záznam v indexu zůstane aktuální.
Co se ale stane, pokud dojde ke změně navázaného obsahu, například opakovaně použitelného prvku (reusable content item), který je součástí více stránek?

V takovém případě je potřeba implementovat logiku, která na základě změny v navázaném obsahu vyhodnotí, které další položky v indexu je nutné přeindexovat. K tomu slouží metoda `FindItemsToReindex`. Všechny položky vrácené z této metody budou předány do `MapToElasticSearchModelOrNull(IIndexEventItemModel item)` pro indexaci.

Ukázka implementace této metody:
https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch/-/blob/IN-654-Elastic-search-from-azure/docs/Custom-index-strategy.md?ref_type=heads#keeping-indexed-related-content-up-to-date




#### Registrace Strategie
Aby bylo možné vlastní strategii použít, je potřeba ji zaregistrovat pomocí dependency injection (DI):
```csharp
services.AddKenticoElasticSearch(builder =>
{
    builder.RegisterStrategy<DancingGoatSearchStrategy, DancingGoatSearchModel>(nameof(DancingGoatSearchStrategy));
}, configuration);
```



## 4. Nastavení indexu v administraci XbyK
Dalším krokem je vytvoření samotného indexu v administraci Xperience. To provedete v aplikaci Elastic Search, kterou do systému přidává tato knihovna. Zde nastavíte název indexu, vyberete odpovídající strategii, jazykové varianty, kanály a typy obsahu, které se mají indexovat.

![XbyK create index](/images/xperience-administration-search-index-edit-form.png)

Po vytvoření a nakonfigurování indexu je potřeba provést jeho přegenerování, spuštěním akce Rebuild na stránce List of registered Elastic Search indices.

![XbyK rebuild index](/images/xperience-administration-search-index-list.png)


Po této akci by již měl být index naplněný položkami (dle implementace `DancingGoatSearchStrategy`) a připravený k samotnému vyhledávání a filtrování.


Odkaz na dokumentaci: https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch/-/blob/IN-654-Elastic-search-from-azure/docs/Managing-Indexes.md



## 5. Vyhledávání
Závěrečným krokem je samotná implementace vyhledávání. 

Proveďte vyhledávání s vlastním nastavením "search options" pomocí služby `IElasticSearchQueryClientService`. Určete parametry vyhledávání a vyberte data, která budou získána z Elasticsearch indexu.

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

Při implementaci vyhledávání se využívá standardní ElasticsearchClient (.NET Client v8) s možností využít Fluent API nebo Object initializer API. Rozdíl mezi těmito přístupy lze vidět zde https://www.elastic.co/docs/reference/elasticsearch/clients/dotnet/query



## Závěr

Integrace Elasticsearch do Xperience by Kentico rozšiřuje možnosti vyhledávání a nabízí flexibilitu, kterou u jiných fulltextových nástrojů nenajdeme. Tato integrace umožňuje rychlou indexaci, pokročilé dotazování a možnost hostování on-premise, což je velká výhoda pro uživatele, kteří chtějí mít plnou kontrolu nad svými daty. S dalším rozvojem technologie se dá očekávat, že se Elasticsearch stane ještě atraktivnějším řešením pro vyhledávací požadavky v Xperience by Kentico.

Doufáme, že vám tento článek pomohl lépe se zorientovat v integraci Elasticsearch do Xperience by Kentico. Možnosti této integrace ale sahají ještě dál, než bylo uvedeno v tomto článku. V dokumentaci integrace najdete například i ukázku pro inspiraci při [crawlování stránek](https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch/-/blob/IN-654-Elastic-search-from-azure/docs/Scraping-web-page-content.md?ref_type=heads#scraping-web-page-content) nebo při správě indexových [aliasů](https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch/-/blob/IN-654-Elastic-search-from-azure/docs/Managing-Aliases.md?ref_type=heads#managing-aliases).


Detailnější návod k vytvoření vlastní indexační strategie (včetně code snippets), způsobu mapování dat a propojení s Kentico Xperience, naleznete přímo v oficiálním repozitáři knihovny. Odkaz na GitLab repozitář: https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch.
