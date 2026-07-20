Before we migrated from Azure to non Azure dependencies, this was the architectural flow - 

# vedastro/vedastro Architecture

## Diagram 1
flowchart TB

%% ===========================
%% User Layer
%% ===========================

subgraph UI["User Interaction"]
    UI1["User Interfaces<br/>(Desktop, Web, Mobile)"]
end

%% ===========================
%% Backend
%% ===========================

subgraph CORE["Core Backend System"]

    API["VedAstro API Gateway<br/>(Azure Functions)"]

    ENGINE["Core Astrological Engine<br/>(VedAstro.Library)"]

    AI["AI / ML & Data Processing<br/>(ML Pipelines, LLM Integration)"]

    API --> ENGINE
    ENGINE --> AI
end

%% ===========================
%% Operations
%% ===========================

subgraph OPS["System Operations"]
    DEVOPS["DevOps & Automation Tools<br/>(Publisher, Generators, Scrapers)"]
end

%% ===========================
%% Data Layer
%% ===========================

subgraph DATA["Data & External Ecosystem"]

    EXT["External Platforms<br/>(Google, Facebook, LLMs, CDN)"]

    STORE["Persistent Data Store<br/>(Azure Table / Blob Storage)"]

end

%% ===========================
%% Connections
%% ===========================

UI1 <--> API

DEVOPS --> API
DEVOPS --> ENGINE
DEVOPS --> STORE

AI --> EXT
AI --> STORE

API --> STORE
ENGINE --> STORE

API --> EXT



## Diagram 2 - This version groups related components, making it easier to understand at a glance.
flowchart LR

subgraph Clients
    UI["Desktop / Web / Mobile"]
end

subgraph Backend
    API["API Gateway"]
    Engine["VedAstro Library"]
    AI["AI / ML Processing"]

    API --> Engine
    Engine --> AI
end

subgraph Infrastructure
    Storage["Azure Table / Blob Storage"]
    External["Google, Facebook, LLMs, CDN"]
    DevOps["Publishers<br/>Generators<br/>Scrapers"]
end

UI <--> API

API --> Storage
Engine --> Storage
AI --> Storage

API --> External
AI --> External

DevOps --> API
DevOps --> Engine
DevOps --> Storage


The VedAstro repository provides an engine that performs Vedic astrological calculations and generates event predictions. It centralizes the data structures, algorithms, caching, and external integrations necessary for these computations.

The engine defines foundational data structures that represent astrological concepts, such as planetary positions and event definitions. It implements algorithms for calculating planetary positions, Dasa periods, divisional charts, and electional astrology. The system also generates various visual astrological charts and reports, including animated GIFs. Its management of geographical locations and timezones supports accurate astrological computations. This includes leveraging caching and efficient storage. Furthermore, the engine provides mechanisms for defining and calculating astrological events, standardizing their logic through delegation.

A centralized API manages astrological calculations, user data, authentication, and logging, primarily utilizing Azure Table Storage for data persistence. This API includes mechanisms for controlling request volume and ensuring fair usage. For more information, see API Services and Data Management.

The project offers a multi-operating system desktop application and web frontends. The desktop application manages the lifecycle of an embedded API server. Web applications, built with Blazor WebAssembly, offer user authentication and access to astrological calculations and prediction tools.

Machine learning components generate data and classify astrological patterns for compatibility predictions. The system integrates with the Hugging Face Hub to manage and utilize extensive planetary data for question-answering tasks. It also processes unstructured text, extracting content from PDF documents and preparing it for text embeddings.

Various utility and automation tools support development and operations:

A console application performs advanced astrological calculations, including finding optimal birth times.
An LLM-powered assistant facilitates coding by interacting with large language models.
Tools migrate geographical and timezone data from external sources.
A web scraper gathers astrological data for famous individuals from public websites.
Automation generates static code artifacts such as API method metadata and data tables.
Automated deployment and publishing processes manage content delivery to Azure Blob Storage and Azure CDN. This includes synchronizing files, injecting file hashes for cache control, and invalidating CDN caches to ensure users receive the latest application updates.

### Astrological Calculation and Prediction Engine

### Diagram 3
flowchart TD

    %% Input Layer
    INPUT["Input Data<br/>(Person, Time, Location)"]

    %% Core Library
    CORE["Core Library<br/>(Data Structures, Algorithms)"]

    %% Calculation Engine
    CALC["Event/Horoscope Calculation<br/>(EventCalculatorMethods, Calculate)"]

    %% Supporting Components
    EXT["External Services<br/>(Azure Tables, Google API, LLM)"]

    CACHE["Caching & Persistence<br/>(Azure Blob/Table Storage)"]

    OUTPUT["Event Prediction Output"]

    %% Main Flow
    INPUT -->|Uses| CORE
    CORE -->|Feeds| CALC

    %% Calculation Interactions
    CALC -->|Integrates| EXT
    CALC -->|Stores/Retrieves| CACHE
    CALC -->|Generates| OUTPUT

    %% External Services
    EXT -->|Stores/Retrieves| CACHE

    %% Cached Data reused by calculations
    CACHE -->|Provides| CALC


## Diagram 4 - This version groups related components, making it easier to understand at a glance.
flowchart TB

subgraph Input
    INPUT["Person<br/>Time<br/>Location"]
end

subgraph Core
    LIB["Core Library"]
    CALC["Event / Horoscope Engine"]
end

subgraph Services
    EXT["External APIs<br/>Azure Tables<br/>Google API<br/>LLMs"]
    CACHE["Azure Blob/Table Storage"]
end

subgraph Output
    RESULT["Prediction Output"]
end

INPUT -->|Input| LIB
LIB -->|Feeds| CALC

CALC -->|Uses| EXT
EXT -->|Read/Write| CACHE

CALC -->|Read/Write| CACHE
CACHE -->|Cached Data| CALC

CALC -->|Produces| RESULT

The VedAstro project's core is an astrological calculation and prediction engine, housed primarily within the Library directory. This engine centralizes the data structures, algorithms, caching mechanisms, and external integrations necessary for Vedic Astrology computations. Its fundamental purpose is to combine astrological logic and data over time to generate event predictions.

At the heart of the system are core data structures that represent astrological concepts and entities. These include Constellation for celestial positions, Dasa for planetary periods, and Bhinnashtakavarga for benefic points in zodiac signs. These structures are designed for serialization and deserialization, often to and from JSON or XML, as seen in classes like Bhinnashtakavarga or EventData. This allows for flexible data representation and interchange within the application and with external services. Specialized entities such as BodyInfoDatasetEntity, LifeEventRow, and PersonListEntity are defined within the Library/Data/AzureTable directory to interface with Azure Table Storage, providing persistence for astrological data, user information, and application-specific statistics. This integration is managed centrally by the static AzureTable class in Library/Data/AzureTable.cs, which provides TableClient instances for various data tables.

A crucial aspect of the engine is the management of geographical locations and timezones, which is handled by LocationManager in Library/Logic/Calculate/LocationManager.cs. This component is responsible for converting geographical addresses, IP addresses, or coordinates into precise geological locations and accurately determining timezones, ensuring the precision of astrological calculations.

The engine also incorporates robust caching mechanisms. The CacheManager in Library/Logic/CacheManager.cs manages in-memory caches that can persist to disk, while AzureCache in Library/Logic/AzureCache.cs extends this by supporting caching data in Azure Blob Storage. This approach minimizes redundant computations and accelerates data retrieval. The CacheKey class in Library/Data/CacheKey.cs plays a key role here by generating unique identifiers for method calls based on their names and arguments, facilitating efficient cache lookup.

Event management and delegation are fundamental to how astrological predictions are structured. The Event class in Library/Data/Event.cs is the base representation of a temporal event. These events are categorized and calculated using delegates like EventCalculatorDelegate defined in Library/Data/Delegate/CalculatorDelegates.cs, which standardize the logic for event and horoscope calculations. The EventManager in Library/Logic/EventManager.cs then orchestrates the calculation and processing of these events over specified time periods.

The system's core astrological algorithms reside in the Library/Logic/Calculate directory. This includes a partial Calculate class in Library/Logic/Calculate/Core.cs for foundational planetary and house calculations, Ashtakavarga for specific chart types, VimshottariDasa for planetary period calculations, and Muhurtha methods for electional astrology. The Vargas class handles divisional charts, while Numerology provides numerological computations. AutoCalculator in Library/Logic/AutoCalculator.cs dynamically discovers and executes these calculation methods using reflection, allowing for extensible prediction logic.

External integrations are managed through components such as CalendarManager in Library/Logic/CalendarManager.cs, which allows interaction with Google Calendar for adding or retrieving events. The ChatAPI in Library/Logic/Calculate/ChatAPI.cs integrates with Large Language Models (LLMs) to generate astrological predictions and text embeddings, a functionality further supported by LLMEmbeddingManager in Library/Logic/LLMEmbeddingManager.cs. The APIFunctionResult in Library/Logic/APIFunctionResult.cs standardizes the output of API function calls.

Logging and error reporting are handled by LogManager in Library/Logic/LogManager.cs and LibLogger in Library/Logic/LibLogger.cs, which provide debugging capabilities and capture critical errors. AlertText in Library/Logic/AlertText.cs centralizes all alert messages for consistent communication.

Utility functions are extensive, ranging from string manipulation and time calculations in Tools in Library/Logic/Tools.cs to GIF processing through the Library/Logic/GIFConverter directory, enabling the creation of animated astrological charts. The concept of AdvancedOptionAttribute in Library/Data/AdvancedOptionAttribute.cs enables marking certain features for advanced users, guiding UI presentation.

## Astrological Data Structures

### Diagram 5
flowchart TD

subgraph Visualization
    CHART["Chart Types<br/>• DasaChart<br/>• ChartOptions<br/>• Bhinnashtakavarga"]
end

subgraph Events
    EVENT["Event Models<br/>• EventData<br/>• Event<br/>• DasaEvent<br/>• CalculatorResult<br/>• DegreeRange"]
end

subgraph Domain
    CORE["Core Astrology Types<br/>Angles<br/>Locations<br/>Planets<br/>Signs<br/>Constellations<br/>Relationships<br/>Horoscope Types"]
end

subgraph Infrastructure
    SERIAL["Serialization Interfaces<br/>IToJson<br/>IFromUrl"]
end

CHART -->|"Uses Event Models"| EVENT
EVENT -->|"Uses Domain Types"| CORE

CHART -.->|"Implements"| SERIAL
EVENT -.->|"Implements"| SERIAL
CORE -.->|"Some Types Implement"| SERIAL

The core data structures within the VedAstro project provide representations for astrological concepts, astronomical bodies, and calculation results. These structures are designed to be serialized and deserialized for internal processing and external data exchange, ensuring data integrity and consistency across various components of the engine.

Key data structures are located primarily in the Library/Data and Library/Data/Enum directories.

Astrological Concepts:

Central to representing astrological information are structures such as Bhinnashtakavarga (Library/Data/Bhinnashtakavarga.cs), which stores planetary benefic points in a 7x12 table format. The Constellation struct (Library/Data/Constellation.cs) encapsulates a specific point within a constellation, including its name, quarter, and precise degree. Similarly, Dasa (Library/Data/Dasa.cs) defines ruling planetary periods for hierarchical astrological analyses. The DegreeRange struct (Library/Data/DegreeRange.cs) provides a flexible way to represent angular ranges, often used in astrological calculations.

Various enumerations in Library/Data/Enum define a standardized vocabulary for astrological elements:

AnimalName (Library/Data/Enum/Animal.cs) for Yoni calculations.
Avasta (Library/Data/Enum/Avasta.cs) for planetary states.
ConstellationName (Library/Data/Enum/ConstellationName.cs) for sidereal constellations.
ZodiacName (Library/Data/Enum/ZodiacName.cs) for the twelve zodiac signs.
PlanetMotion (Library/Data/Enum/PlanetMotion.cs) describes planetary movements (e.g., Retrograde, Direct).
PlanetToPlanetRelationship (Library/Data/Enum/PlanetToPlanetRelationship.cs) and PlanetToSignRelationship (Library/Data/Enum/PlanetToSignRelationship.cs) categorize astrological relationships between celestial bodies and zodiac signs.
Ayanamsa (Library/Data/Enum/Ayanamsa.cs) and SimpleAyanamsa (Library/Data/Enum/SimpleAyanamsa.cs) define different precession correction methods.
ChartType (Library/Data/Enum/ChartType.cs) specifies various astrological chart types (e.g., Rasi, Navamsha).
EventName (Library/Data/Enum/EventName.cs) and HoroscopeName (Library/Data/Enum/HoroscopeName.cs) categorize astrological events and calculations.
EventTag (Library/Data/Enum/EventTag.cs) provides a system for categorizing events.
Event and Chart Representation:

The Event class (Library/Data/Event.cs) is a fundamental structure representing temporal events with properties such as name, nature, description, start and end times, and associated tags. It includes methods for calculating event duration and determining if a specific time falls within the event's duration. For example, GetRelatedPlanet and GetRelatedHouse extract relevant astrological entities directly from the event's name. The EventData struct (Library/Data/EventData.cs) further extends this by encapsulating comprehensive event information, including an EventCalculatorDelegate for its associated calculation logic.

DasaEvent (Library/Data/DasaEvent.cs) acts as a specialized wrapper for Event objects, adding Dasa-specific properties such as the Dasa level, name, and planetary lords.

ChartOptions (Library/Data/ChartOptions.cs) manages configuration for astrological chart generation, specifically defining which calculation algorithms are selected. DasaChart (Library/Data/DasaChart.cs) represents an astrological events report chart, encapsulating data needed for its display and storage, including the associated person, time range, event tags, and display options.

Fundamental Data Types and Utilities:

The Angle class (Library/Data/Angle.cs) is a fundamental component for representing and manipulating angular measurements in degrees, minutes, and seconds. It supports conversions, normalization, and arithmetic operations crucial for astronomical precision. GeoLocation (Library/Data/GeoLocation.cs) represents geographical coordinates and names, supporting serialization and deserialization.

The CalculatorResult class (Library/Data/CalculatorResult.cs) encapsulates the outcome of astrological calculations, indicating whether an event is occurring and allowing for overrides of its nature and description.

Serialization and Deserialization:

Many of these data structures implement IToJson and IFromUrl interfaces or provide dedicated serialization methods, facilitating their conversion to and from JSON objects and URL query parameters. This standardization supports data interchange across different modules and API endpoints. For example, ToJson within Event and ToJsonList within DasaEvent handle the conversion of objects. The ChartName struct (Library/Data/ChartName.cs) includes FromXml for deserialization from XML sources, showing support for multiple data formats.

The CacheHolder class (Library/Data/CacheHolder.cs) is a serializable container for a CacheKey (Library/Data/CacheKey.cs) and its associated value. CacheKey provides a unique identifier for method calls based on their names and arguments, which is used for caching computed results.

Other Supporting Structures:

CallerInfo (Library/Data/CallerInfo.cs) manages user and visitor identification, combining them into a unified caller ID. Calendar (Library/Data/Calendar.cs) is a data structure for calendar information, used to identify target calendar services. The Data class (Library/Data/Data.cs) offers a mechanism for managing XML data, providing an abstraction layer for reading, querying, and modifying XML elements from local files or Azure Blob Storage.

Azure Table Storage Integration
Category	Entity Name	Primary Purpose/Data Stored	PartitionKey	RowKey
Astrological Data	BodyInfoDatasetEntity	Stores datasets of celestial body information.	Not specified in code	Time of creation
Astrological Data	MarriageInfoDatasetEntity	Stores marriage event datasets.	Not specified in code	Time of creation
Astrological Data	MarriageTrainingDatasetEntity	Stores data for marriage prediction model training.	Not specified in code	Not specified in code
User Information	LifeEventRow	Stores a person's life events.	Person ID	Time of creation
User Information	PersonListEntity	Stores general person information like birth details.	User ID (Owner ID)	Time of creation (Person ID)
User Information	PersonNameEmbeddingsEntity	Stores embeddings for person names.	User ID (Owner ID)	Person ID
User Information	PersonShareRow	Records shared person profiles between users.	Owner ID of recipient	Person ID of shared profile
User Information	UserDataListEntity	Stores user account data, including API keys and Stripe IDs.	Google/Facebook ID	User email
Application Statistics	APIAbuseRow	Tracks API abuse by IP address.	Caller IP address	Random ID
Application Statistics	*GeoLocation* Entities	Caches and stores geographical location and timezone data for various purposes (e.g., AddressGeoLocationEntity, CoordinatesGeoLocationEntity, GeoLocationTimezoneEntity, IpAddressGeoLocationEntity and their Metadata counterparts).	Location/Coordinate/IP details	Specific search parameters or empty
Application Statistics	*Statistic* Entities	Collects various application usage statistics (e.g., IpAddressStatisticEntity, RawRequestStatisticEntity, RequestUrlStatisticEntity, SubscriberStatisticEntity, UserAgentStatisticEntity, WebPageStatisticEntity).	Relevant statistical key (e.g., IP, URL, User Agent)	Time-based key or empty
Data persistence in the VedAstro project heavily relies on Azure Table Storage for astrological data, user information, and various application statistics. This NoSQL data store is used to manage entities that inherit from ITableEntity, which provides standard properties like PartitionKey, RowKey, Timestamp, and ETag for efficient storage and retrieval.

Astrological data is structured to represent various concepts. For instance, celestial body information is stored using BodyInfoDatasetEntity in Library/Data/AzureTable/BodyInfoDatasetEntity.cs, where the Info property holds JSON-formatted data about astronomical bodies. Life events associated with individuals are modeled by LifeEventRow in Library/Data/AzureTable/LifeEventRow.cs, using Person ID as PartitionKey and Time of creation as RowKey to organize events by person and chronology. Marriage-related information for datasets and training is managed by MarriageInfoDatasetEntity in Library/Data/AzureTable/MarriageInfoDatasetEntity.cs and MarriageTrainingDatasetEntity in Library/Data/AzureTable/MarriageTrainingDatasetEntity.cs, respectively. These entities often store complex data as JSON strings in an Info property, allowing for flexible schemas. For example, MarriageTrainingDatasetEntity includes fields for Outcome, MarriageDate, and MalePersonId, with embeddings stored as a string that can be converted to a double[] for machine learning purposes.

User information is stored to manage profiles and access. PersonListEntity in Library/Data/AzureTable/PersonListEntity.cs stores individual person records, including Name, BirthTime (as JSON), and Gender, with the PartitionKey assigned to the user ID and RowKey to the creation timestamp. PersonNameEmbeddingsEntity in Library/Data/AzureTable/PersonNameEmbeddingsEntity.cs stores numerical embeddings of person names, which are parsed from a string to a double[] array for computational use. PersonShareRow in Library/Data/AzureTable/PersonShareRow.cs tracks shared profiles, linking an ownerId (receiver) to a sharedPersonId via PartitionKey and RowKey. User profiles and subscription details are managed by UserDataListEntity in Library/Data/AzureTable/UserDataListEntity.cs, which stores Name, APIKey, and StripeCustomerID, using the owner's ID and email as keys.

Application-specific statistics, such as API usage and geolocation data, are comprehensively tracked. The directory Library/Data/Statistic contains various entities for this purpose. APIAbuseRow in Library/Data/Statistic/APIAbuseRow.cs records API abuse incidents using the caller's IP address as PartitionKey. Geolocation and timezone data are stored across several entities like AddressGeoLocationEntity in Library/Data/Statistic/AddressGeoLocationEntity.cs, CoordinatesGeoLocationEntity in Library/Data/Statistic/CoordinatesGeoLocationEntity.cs, GeoLocationCacheEntity in Library/Data/Statistic/GeoLocationCacheEntity.cs, GeoLocationTimezoneEntity in Library/Data/Statistic/GeoLocationTimezoneEntity.cs, and IpAddressGeoLocationEntity in Library/Data/Statistic/IpAddressGeoLocationEntity.cs. These entities use various combinations of location names, coordinates, and IP addresses for PartitionKey and RowKey to optimize retrieval. Metadata related to geolocation and timezones is stored in GeoLocationTimezoneMetadataEntity in Library/Data/Statistic/GeoLocationTimezoneMetadataEntity.cs and IpAddressGeoLocationMetadataEntity in Library/Data/Statistic/IpAddressGeoLocationMetadataEntity.cs, both of which include methods to calculate a combined hash for efficient lookup.

API request statistics are detailed in RawRequestStatisticEntity in Library/Data/Statistic/RawRequestStatisticEntity.cs and RequestUrlStatisticEntity in Library/Data/Statistic/RequestUrlStatisticEntity.cs, tracking call counts and headers. IpAddressStatisticEntity in Library/Data/Statistic/IpAddressStatisticEntity.cs monitors call rates at different granularities (second, minute, hour, day, month) for specific IP addresses. Subscriber usage is tracked by SubscriberStatisticEntity in Library/Data/Statistic/SubscriberStatisticEntity.cs using host address and year-month as keys. User agent and webpage access statistics are managed by UserAgentStatisticEntity in Library/Data/Statistic/UserAgentStatisticEntity.cs and WebPageStatisticEntity in Library/Data/Statistic/WebPageStatisticEntity.cs, respectively. Each entity within Library/Data/Statistic includes an Empty static field for default instances and leverages the PartitionKey and RowKey to align with expected query patterns in Azure Table Storage. For more information on API services, refer to API Services and Data Management.

## Core Astrological Algorithms

### Diagram 6 
flowchart TD

    %% ===========================
    %% Core Algorithms
    %% ===========================

    CALC["Core Astrological Algorithms<br/>(Library.Logic.Calculate)"]

    %% ===========================
    %% Calculation Modules
    %% ===========================

    CORE["Core, Muhurta, Numerology"]

    DASA["Vimshottari Dasa,<br/>Vargas, Ashtakavarga"]

    %% ===========================
    %% Shared Services
    %% ===========================

    LOCATION["LocationManager"]

    AI["ChatAPI, NLPTools"]

    %% Relationships

    CALC -->|Utilizes| CORE
    CALC -->|Uses| DASA

    CORE -->|Interacts With| LOCATION
    DASA -->|Interacts With| LOCATION

    LOCATION -->|Feeds| AI


### Diagram 7 - This version groups components by responsibility and scales better as the library grows.
flowchart TB

subgraph Algorithms
    CALC["Library.Logic.Calculate"]
end

subgraph Calculation_Modules
    CORE["Core<br/>Muhurta<br/>Numerology"]

    DASA["Vimshottari Dasa<br/>Vargas<br/>Ashtakavarga"]
end

subgraph Shared_Services
    LOCATION["LocationManager"]
end

subgraph AI_Integration
    CHAT["ChatAPI"]
    NLP["NLPTools"]
end

CALC -->|"Utilizes"| CORE
CALC -->|"Uses"| DASA

CORE -->|"Location Services"| LOCATION
DASA -->|"Location Services"| LOCATION

LOCATION --> CHAT
LOCATION --> NLP

flowchart LR

CALC["Library.Logic.Calculate"]

subgraph Modules
    CORE["Core / Muhurta / Numerology"]
    DASA["Vimshottari Dasa / Vargas / Ashtakavarga"]
end

subgraph Services
    LOCATION["LocationManager"]
end

subgraph AI
    CHAT["ChatAPI"]
    NLP["NLPTools"]
end

CALC --> CORE
CALC --> DASA

CORE -.-> LOCATION
DASA -.-> LOCATION

LOCATION --> CHAT
LOCATION --> NLP

The core astrological algorithms within the engine calculate fundamental astronomical and astrological values crucial for Vedic astrology. These computations cover planetary positions, house assignments, Dasa periods, divisional charts (Vargas), and Muhurta (electional astrology).

Planetary and house calculations, found primarily in Library/Logic/Calculate/Core.cs, delineate relationships such as houses owned by a planet, planets residing in specific houses, and the lord of a given house. This includes determining planetary aspects, conjunctions with benefic or malefic planets, and assessing a planet's strength and relationship within a sign or relative to other planets. Temporal calculations are also handled here, including sunrise/sunset times, the duration of day, and specific astrological timings like IshtaKaala and HoraAtBirth. Many of these calculations employ caching via CacheManager.GetCache for performance optimization.

The system also computes Ashtakavarga charts, detailed in Library/Logic/Calculate/Ashtakavarga.cs. This involves calculating benefic points for planets in different zodiac signs based on their positions relative to other celestial bodies and the Ascendant. It provides methods for computing Prastaraka (individual planet strength), Sarvashtakavarga (collective strength), and Bhinnashtakavarga charts.

Dasa periods, specifically Vimshottari Dasa, are managed by the VimshottariDasa class in Library/Logic/Calculate/VimshottariDasa.cs. This component calculates the hierarchical sequences of planetary periods and sub-periods (up to 8 levels) for a given birth time and current time. It also provides interpretations of the astrological relationships between the major and minor period planets.

Divisional charts, or Vargas, are computed by the Vargas class in Library/Logic/Calculate/Vargas.cs. This functionality determines the zodiac sign and adjusted degrees of a celestial body within various divisional charts (e.g., Hora D2, Navamsha D9) based on precomputed tables.

Muhurta, or electional astrology, calculations are extensively implemented in Library/Logic/Calculate/Muhurtha.cs. This includes determining auspicious or inauspicious timings for various activities like travel, medical treatments, and marriage. These methods assess factors such as planetary positions, lunar days, constellations, and specific astrological combinations (Yogas) like Tarabala, Chandrabala, and Panchaka. The Pancha Pakshi system data, used in some Muhurta calculations, is stored in Library/Logic/Calculate/PanchaPakshi.cs.

Finally, numerological calculations are available in Library/Logic/Calculate/Numerology.cs, which derive BirthNumber, DestinyNumber, and NameNumber based on Chaldean numerology and provide corresponding predictions. The MLDatasetTools in Library/Logic/Calculate/MLDatasetTools.cs serves as a placeholder for future machine learning dataset manipulation, while NLPTools in Library/Logic/Calculate/NLPTools.cs provides basic NLP functions like tokenization and cosine similarity for text analysis. Conversational AI related to these astrological calculations is handled by ChatAPI in Library/Logic/Calculate/ChatAPI.cs, which integrates with Large Language Models and Azure Table Storage to process user queries. Geographical location and timezone management, essential for accurate astrological calculations, is detailed in Geographical Location and Timezone Management.

## Astrological Chart and Report Generation

### Diagram 
flowchart TD

    %% ===========================
    %% Factory Layer
    %% ===========================

    FACTORY["Astrology Factories"]

    %% ===========================
    %% Outputs
    %% ===========================

    SVG["SVG Chart"]

    REPORT["Match Report"]

    GIF["Animated GIF"]

    CONVERTER["GIF Converter"]

    %% Relationships

    FACTORY -->|Produces| SVG
    FACTORY -->|Produces| REPORT
    FACTORY -->|Produces| GIF

    GIF -->|Uses| CONVERTER


### Diagram 
flowchart TB

subgraph Factory
    FACTORY["Astrology Factories"]
end

subgraph Generated_Artifacts
    SVG["SVG Chart"]
    REPORT["Match Report"]
    GIF["Animated GIF"]
end

subgraph Utilities
    CONVERTER["GIF Converter"]
end

FACTORY -->|"Creates"| SVG
FACTORY -->|"Creates"| REPORT
FACTORY -->|"Creates"| GIF

GIF -->|"Rendered by"| CONVERTER
 

### Diagram 
classDiagram

class AstrologyFactories

class SVGChart
class MatchReport
class AnimatedGIF
class GIFConverter

AstrologyFactories --> SVGChart : creates
AstrologyFactories --> MatchReport : creates
AstrologyFactories --> AnimatedGIF : creates

AnimatedGIF --> GIFConverter : uses

The project employs a factory pattern to generate various astrological charts and comprehensive reports. These outputs are primarily rendered in SVG format, with support for animated GIFs for dynamic visualizations.

For instance, the EventsChartFactory within Library/Logic/Factory/EventsChartFactory.cs is designed to create SVG charts visualizing astrological event timelines. This involves orchestrating the creation of various SVG components, such as headers, event rows, life events, summary data, and a "now" line, which are then assembled into a complete SVG string. The process includes calculating event data, managing vertical positioning of elements to prevent overlap, and embedding JavaScript for interactive functionalities.

Compatibility reports are handled by MatchReportFactory in Library/Logic/Factory/MatchReportFactory.cs. This component calculates Vedic astrology compatibility between two individuals by assessing numerous astrological factors, known as Kutas. It aggregates individual predictions into a comprehensive report, determines a total compatibility score, and generates machine learning embeddings from the results. The factory also applies astrological exception rules to modify predictions based on specific conditions.

Visual representations of the sky and planetary positions are generated by SkyChartFactory in Library/Logic/Factory/SkyChartFactory.cs. This factory produces SVG sky charts that include zodiac signs, houses, and planets for a given time and location. It can also generate animated GIFs by creating a sequence of SVG frames over a time duration, converting these SVGs to PNGs, and then assembling them into a GIF. This process involves generating 360-degree angle rulers, scaling zodiac images, and calculating house positions.

North Indian style astrological charts are generated by NorthChartFactory in Library/Logic/Factory/NorthChartFactory.cs. This factory produces SVG charts by placing planet and house positions onto a fixed graphical background. It manages the positioning of celestial bodies within house display areas, ensuring proper stacking when multiple planets occupy the same house. Similarly, SouthChartFactory in Library/Logic/Factory/SouthChartFactory.cs creates South Indian style astrological charts in SVG format, embedding zodiac signs and planetary positions onto a standard background structure. Both North and South Indian chart factories rely on external calculation utilities to determine precise astrological positions.

The generation of animated GIFs from sequences of images is managed by components within Library/Logic/GIFConverter. Specifically, AnimatedGifEncoder.cs handles the encoding process, converting a sequence of image frames into an animated GIF. It offers control over animation parameters such as frame delay and repeat count, and utilizes color quantization via NeuQuant.cs and LZW compression via LZWEncoder.cs to create the final GIF output. Conversely, GifDecoder.cs is responsible for parsing existing GIF files and extracting individual frames and associated metadata, including display delays and loop counts.

## Geographical Location and Timezone Management

### Diagram
flowchart TD

    IPGeo["IP Address GeoLocation Entity"]
    SearchGeo["Search GeoLocation Entity"]

    IPMeta["IP Address GeoLocation Metadata Entity"]

    AddressGeo["Address & Coordinates<br/>GeoLocation Entities"]

    Timezone["GeoLocationTimezone Entity"]

    TimezoneMeta["GeoLocationTimezoneMetadata Entity"]

    IPGeo -->|Links to Metadata| IPMeta
    IPGeo -->|Derives Location| AddressGeo

    SearchGeo -->|Stores Search Results| AddressGeo

    AddressGeo -->|Provides Location Data| Timezone

    Timezone -->|Links to Metadata| TimezoneMeta


The system manages geographical locations and timezones primarily through Azure Table Storage entities to support accurate astrological calculations. These entities capture and store diverse location data, from broad addresses to precise coordinates, and integrate timezone information.

The AddressGeoLocationEntity in Library/Data/Statistic/AddressGeoLocationEntity.cs stores geographic location and timezone information, using a full location name as its PartitionKey and a cleaned user-entered name as its RowKey. This enables efficient lookup and conversion of address-based queries into precise GeoLocation objects. For coordinate-based queries, the CoordinatesGeoLocationEntity in Library/Data/Statistic/CoordinatesGeoLocationEntity.cs uses latitude as its PartitionKey and longitude as its RowKey, optimizing for spatial searches.

To enhance performance and reduce redundant external API calls, location data is extensively cached. The GeoLocationCacheEntity in Library/Data/Statistic/GeoLocationCacheEntity.cs stores cached geographical location information, using the location name as the PartitionKey and a time offset or caller-provided name as the RowKey. This allows for rapid retrieval of previously requested location data.

IP addresses are also used to determine geographical locations. The IpAddressGeoLocationEntity in Library/Data/Statistic/IpAddressGeoLocationEntity.cs links IP addresses to geographical location data, with the IP address itself serving as the PartitionKey. Further details about IP address geolocation, including threat metadata, are stored in the IpAddressGeoLocationMetadataEntity in Library/Data/Statistic/IpAddressGeoLocationMetadataEntity.cs, which generates an MD5 hash of its properties for efficient retrieval.

Timezone information is crucial for astrological accuracy. The GeoLocationTimezoneEntity in Library/Data/Statistic/GeoLocationTimezoneEntity.cs stores timezone data based on geographical coordinates, using a combined latitude/longitude string as its PartitionKey. Detailed metadata for timezones, including standard offsets and daylight saving information, is managed by the GeoLocationTimezoneMetadataEntity in Library/Data/Statistic/GeoLocationTimezoneMetadataEntity.cs, which also uses a hash for its PartitionKey to identify unique timezone configurations.

Search functionality for addresses is supported by SearchAddressGeoLocationEntity in Library/Data/Statistic/SearchAddressGeoLocationEntity.cs, which caches search queries and their corresponding GeoLocation results as JSON, allowing for quick retrieval of past search outcomes.

The overall approach ensures that the system can accurately convert various forms of geographical input into precise locations and timezones, leveraging caching and efficient storage mechanisms for astrological calculations. For more detailed insights into the persistence mechanisms, refer to Azure Table Storage Integration.

## Event Management and Delegation

### Diagram
flowchart TD

    DT["Delegate Types"]

    HC["Horoscope Calculators"]
    EC["Event Calculators"]
    EG["Event Generators"]
    AF["Algorithm Functions"]

    CR["CalculatorResult"]
    EPT["Event/Person/Time Data"]

    DT -->|standardizes| HC
    DT -->|standardizes| EC
    DT -->|standardizes| EG
    DT -->|standardizes| AF

    HC --> CR

    EC --> CR
    EC --> EPT

    EG --> EPT

    AF --> EPT


Astrological event management and delegation within the system are primarily facilitated through the use of delegates, which standardize the signatures for various calculation and generation methods. This approach allows for consistent invocation of diverse algorithms, whether for event charts, Muhurtha calculations, or general horoscope computations.

For instance, the AlgorithmFuncs delegate, defined in Library/Data/Delegate/AlgoritmFuncs.cs, standardizes functions that analyze events and individuals, returning a numerical result. Similarly, EventCalculatorDelegate and HoroscopeCalculatorDelegate, found in Library/Data/Delegate/CalculatorDelegates.cs, establish common interfaces for methods that calculate Muhurtha events and horoscopes respectively. These delegates ensure that all such calculation methods adhere to a uniform input (time and/or person objects) and output (CalculatorResult) structure.

Furthermore, the EventGenerator delegate, specified in Library/Data/Delegate/EventGenerator.cs, provides a blueprint for functions responsible for identifying or determining specific astrological events based on a given time and person. This enables a standardized mechanism for event identification logic across the system. These delegates are crucial for enabling a modular and flexible system where different calculation or event generation logics can be interchanged or extended while maintaining consistent interfaces.

## API Services and Data Management

### Diagram
flowchart TB

    API["Azure Functions API"]

    subgraph FA["Functional Areas"]
        direction TB

        CFD["Calculations & FrontDesk"]
        LM["Logging & Monitoring"]

        UM["User Management"]
        TH["Throttling"]
    end

    EAP["External Auth Providers"]
    ATS["Azure Table Storage"]

    API -->|Routes to| CFD
    API -->|Sends logs to| LM

    CFD -->|Uses| UM
    CFD -->|Applies rate limits| TH
    CFD -->|Reads/Writes Data| ATS

    UM -->|Authenticates via| EAP

    TH -->|Checks records in| ATS

The VedAstro project employs a centralized API, primarily implemented as an Azure Functions application located in the API directory, to manage astrological calculations, user data, authentication, messaging, and logging. This API is designed for scalability and cost-efficiency through its serverless architecture.

Data persistence within the API relies heavily on Azure Table Storage, utilized for various data types including analytics, call status, geolocation caching, error logging, and general API logs. For instance, API/TableData/AnalyticsEntity.cs defines structures for storing analytical data. This choice provides a scalable and cost-effective solution for structured, schemaless data. Caching mechanisms, such as those detailed in Azure Table Storage Integration, are integrated to improve performance by storing frequently accessed data and charts.

Authentication for users is managed through integrations with Google and Facebook, allowing users to sign in and register their accounts. Upon sign-in, the API handles the persistence of user data, including personal profiles, to Azure Table Storage. The API also includes mechanisms for throttling and rate limiting to prevent abuse and ensure fair usage across anonymous and subscribed users, with configurable thresholds for call rates.

Error reporting and logging are comprehensively handled within the API. A custom logger, defined in API/ApiLogger.cs, automatically logs API errors to Azure Table Storage. Furthermore, client-side errors and debug information from web applications are captured and stored, providing a detailed record for troubleshooting.

The API's deployment and execution environment is defined by a Dockerfile in API/Dockerfile. This Dockerfile outlines a multi-stage build process that optimizes the final image size by separating build and runtime dependencies, preparing the application for deployment to Azure Functions.

API Endpoint Design and Implementation
The API design centralizes astrological calculations, user data management, and operational logging. It is implemented as an Azure Functions application, utilizing a serverless execution model for scalability and efficiency. Requests are routed through various endpoints, each designed to handle specific functionalities such as dynamic astrological computations, user authentication, and data persistence.

The core of the API's extensibility for astrological calculations is managed by API/FrontDesk/OpenAPI.cs. This file exposes a central Calculate endpoint that dynamically invokes diverse astrological computations. It parses parameters from URLs, handles Ayanamsa settings, and includes mechanisms for "all" calls, which systematically process computations for collections of entities like all planets or all houses. This dynamic invocation relies on reflection to identify and execute the appropriate calculation methods based on the incoming request.

User and personal data management is handled through specific API endpoints. API/FrontDesk/PersonAPI.cs provides functionality for adding, updating, deleting, and retrieving person records. It also manages data migration for users who initially interact as visitors and later log in, ensuring their associated data is transferred to their authenticated accounts. API Authentication and User Management further details user authentication processes, including sign-in via Google and Facebook, and the management of user profiles.

Error handling and logging are integrated across the API. The APILogger in API/ApiLogger.cs provides a mechanism for logging API errors to Azure Table Storage, capturing details like the caller's IP address, URL, and error messages. Additionally, API/FrontDesk/WebsiteLoggerAPI.cs offers endpoints for capturing client-side website errors and debug information, contributing to comprehensive error reporting.

The API also includes specialized functionalities such as API/FrontDesk/BirthTimeFinderAPI.cs for astrological birth time rectification and API/FrontDesk/EventsChartAPI.cs for generating and managing astrological event charts. The EventsChart endpoint leverages caching to improve performance by storing frequently accessed charts. API/FrontDesk/MatchAPI.cs provides astrological matchmaking capabilities, calculating compatibility scores between individuals.

Overall, the API architecture is designed to support a wide range of astrological services, with a strong emphasis on dynamic execution, data management, and robust error handling within a serverless Azure Functions environment. For details on how data is stored, refer to Data Persistence with Azure Table Storage.

Data Persistence with Azure Table Storage
C# Class Name	File Path	Purpose	PartitionKey	RowKey
AnalyticsEntity	/vedastro/vedastro/API/TableData/AnalyticsEntity.cs	Stores analytics data for API usage.	string	string
CallStatusEntity	/vedastro/vedastro/API/TableData/CallStatusEntity.cs	Tracks the status of long-running API calls.	string	string
GeoLocationCacheEntity	/vedastro/vedastro/API/TableData/GeoLocationCacheEntity.cs	Caches geographical location data used by the API.	string (Location Name)	string (Date/Time Offset in Ticks OR Location Name)
OpenAPIErrorBookEntity	/vedastro/vedastro/API/TableData/OpenAPIErrorBookEntity.cs	Records errors encountered during OpenAPI calls.	string (IP Address)	string (UTC Call Time in Ticks)
OpenAPILogBookEntity	/vedastro/vedastro/API/TableData/OpenAPILogBookEntity.cs	Logs details of OpenAPI calls, including request body and headers.	string	string
Azure Table Storage is extensively utilized for data persistence across various functionalities within the API, providing a scalable and cost-effective NoSQL solution. The directory API/TableData defines the C# classes that implement the ITableEntity interface, structuring data for storage and retrieval in Azure Tables.

For analytics, the AnalyticsEntity in API/TableData/AnalyticsEntity.cs captures data such as URLs, providing PartitionKey and RowKey for efficient querying. The status of long-running asynchronous operations, like EventsChart, is tracked using CallStatusEntity in API/TableData/CallStatusEntity.cs, which includes an IsRunning flag to indicate active processes.

Geographical location data is cached using GeoLocationCacheEntity in API/TableData/GeoLocationCacheEntity.cs. This entity stores details such as Timezone, Longitude, Latitude, and a Source identifier, optimizing search and retrieval with PartitionKey (location name) and RowKey (date-time offset or name).

Error logging is managed by OpenAPIErrorBookEntity in API/TableData/OpenAPIErrorBookEntity.cs. This entity records detailed error information, including the IP address (PartitionKey), UTC call time (RowKey), code branch, URL, and a compiled error message, to facilitate debugging. General API log entries are structured by OpenAPILogBookEntity in API/TableData/OpenAPILogBookEntity.cs, which can store URLs, request bodies, and up to 15 header fields.

Beyond these core entities, Azure Table Storage also underpins critical API functionalities such as API throttling and user management. For instance, ThrottleManager utilizes AzureTable.AnonymousIpCallRecords and AzureTable.SubscriberCallRecords to persist call statistics for anonymous users and subscribers, respectively, enabling rate limiting. User data, including API keys and authentication details from Google and Facebook sign-ins, is stored and managed within UserDataList via the API Authentication and User Management services. Client-side website errors and debug information are captured as WebsiteErrorLogEntity and WebsiteDebugLogEntity in WebsiteErrorLog and WebsiteDebugLog tables, as described in API Logging and Error Reporting. Person profile data is persistently stored in AzureTable.PersonList, with PartitionKey set to ownerId and RowKey to personId for efficient access and management.

API Throttling and Rate Limiting
API throttling and rate limiting are implemented to manage the volume of incoming requests, ensuring system stability and preventing abuse. The system differentiates between requests originating from web browsers, those authenticated with a valid API key, and anonymous requests based on IP addresses. Browsers and authenticated API key users are generally allowed to operate at full speed. Anonymous requests, however, are subject to rate limiting based on a configurable threshold.

The core logic for managing API call rates is handled within the ThrottleManager static class, specifically the HandleCall method in API/ThrottleManager.cs. This method first determines if a request comes from a browser by inspecting the User-Agent header. If it's a browser, the request proceeds without rate limiting. Otherwise, the system attempts to extract an API key from the request parameters. If a valid API key is present, the call is recorded as a subscriber call via RecordAPISubscriberCall in API/ThrottleManager.cs and allowed to proceed. If no valid API key is found, the request undergoes IP-based throttling.

For anonymous IP requests, a configurable threshold, retrieved from environment settings via GetCallCountThresholdFromSettings in API/ThrottleManager.cs, dictates the allowed number of calls within a 60-second window. Each anonymous call is recorded in Azure Table Storage using RecordAnonymousIPCall in API/ThrottleManager.cs. If the number of calls from a specific IP address within the last 60 seconds exceeds the threshold, the system introduces a delay before processing the request. This approach ensures that while legitimate usage is unhindered, excessive requests from unauthenticated sources are managed to maintain service quality. The call records, for both anonymous IPs and API subscribers, are persistently stored in Azure Table Storage, leveraging AnonymousIpCallRecords and SubscriberCallRecords as described in Data Persistence with Azure Table Storage.

API Authentication and User Management
User authentication and data management are handled through a set of API endpoints designed for user sign-in and profile maintenance. The system supports authentication via Google and Facebook, allowing users to leverage their existing accounts.

For user sign-in, the API provides endpoints for both Google and Facebook authentication. SignInGoogle in API/FrontDesk/SignInAPI.cs validates Google ID tokens to verify user identity, while SignInFacebook in the same file processes Facebook access tokens to retrieve user details. After successful validation, user information such as ID, name, and email is either added as a new record or updated in the system's database via AddOrUpdateUserData. Additionally, a FacebookDeauthorize endpoint is provided for Facebook's deauthorization callbacks, logging such events.

User and personal data, including person profiles, are managed through dedicated APIs. The PersonAPI in API/FrontDesk/PersonAPI.cs facilitates operations such as adding, updating, and deleting Person records. This system also handles the migration of person data from temporary "visitor" accounts to permanent user accounts upon login, ensuring that any data generated by a guest is retained. Access restrictions are in place to prevent unauthorized modifications to public profiles.

The system also manages the association of API keys with user accounts. The SubscriptionAPI in API/FrontDesk/SubscriptionAPI.cs allows for the registration or updating of an apiKey linked to a specific ownerId. This mechanism helps track API usage and manage access for subscribers, as detailed in API Throttling and Rate Limiting. Data persistence for user information, API keys, and person profiles relies on Azure Table Storage, as described in Data Persistence with Azure Table Storage.

API Logging and Error Reporting
The API employs a comprehensive logging system for tracking both errors and general activity across its services. This system is crucial for monitoring API health, debugging issues, and understanding usage patterns.

API errors are centrally logged to Azure Table Storage via the APILogger class located in API/ApiLogger.cs. This logger captures detailed exception information, including the caller's IP address, the URL of the failing request, the code branch (e.g., beta/stable), and a JSON representation of the exception details. This data is stored in OpenAPIErrorBook as OpenAPIErrorBookEntity objects, allowing for efficient retrieval and analysis of error trends. The OpenAPIErrorBookEntity in API/TableData/OpenAPIErrorBookEntity.cs uses the IP address as its PartitionKey and the UTC call time in ticks as its RowKey for optimized querying.

Beyond errors, the system also records general API activity. These log entries, represented by OpenAPILogBookEntity in API/TableData/OpenAPILogBookEntity.cs, capture details such as the URL accessed, the request body, and up to 15 generic header fields. This provides a detailed trail of interactions with the API, which can be invaluable for auditing and performance analysis.

Furthermore, the system extends its logging capabilities to client-side applications, such as the website. The API/FrontDesk/WebsiteLoggerAPI.cs endpoint provides dedicated functions for capturing client-side errors (LogError) and debug information (LogDebug). These functions receive JSON payloads from the client, which are then mapped to WebsiteErrorLogEntity and WebsiteDebugLogEntity respectively, and stored in AzureTable.WebsiteErrorLog and AzureTable.WebsiteDebugLog. These entities, found in API/FrontDesk/WebsiteLoggerAPI.cs, track user ID, local time, URL, error messages, stack traces, and user agents, offering critical insights into front-end issues.

Desktop and Web Applications
The project incorporates a multi-OS desktop application and several web frontends that provide astrological calculations, predictions, and user management features.

The desktop application, based on .NET MAUI, supports various operating systems including macOS, Windows, and Linux. This application is designed to manage the lifecycle of an API server, which is the core astrological calculation engine. For instance, the APILauncher in Desktop/APILauncher is responsible for starting and managing the API server process, including checking for and installing necessary .NET runtimes. The desktop application also handles platform-specific configurations and build processes. For example, Desktop/MAUIAppPublisher.ps1 automates the build and publishing of the Windows version of the application, while the Desktop/MacOS directory contains specific tooling for macOS, including an application launcher to monitor the Azure Functions CLI and manage Python dependencies. The Desktop/Windows directory defines a Windows Forms application that includes UI definitions, application lifecycle management, and control over the Azure Functions CLI process.

Client-side functionalities for the desktop application are primarily handled by a Blazor WebAssembly frontend located in Desktop/wwwroot. This Blazor application serves static web content, provides client-side scripting, and styling. It extensively uses JavaScript interop, as seen in Desktop/wwwroot/js/Interop.js and Desktop/wwwroot/js/VedAstro.js, to bridge .NET and JavaScript functionalities for UI manipulation, external library integration, and data visualization.

In addition to the desktop application, the project features web frontends, including the main website in Website and a mobile-optimized version in Website_Mobile. These Blazor WebAssembly applications offer functionalities such as user authentication, various astrological calculations (horoscope generation, predictions, compatibility checks, and optimal birth time finding), and informational pages. The main website's wwwroot directory in Website/wwwroot contains static assets, client-side scripts, and styling. It integrates numerous external JavaScript libraries for UI/UX enhancements and API interactions. The mobile website, found in Website_Mobile, is structured with components for dynamic content, astrological tools, and user authentication, leveraging third-party libraries for an enhanced user experience. Both web frontends handle user authentication, often integrating with services like Facebook and Google, as demonstrated in Website/wwwroot/index.html.

Desktop Application Architecture and API Management
The Desktop application within the VedAstro project implements a multi-OS architecture, allowing the application to run natively across different operating systems while managing the lifecycle of an associated API server. This design, outlined in Desktop/README.md, aims for native experiences on macOS (using SwiftUI), Windows (using C# WinForms and .NET 8), and potentially Linux, all interacting with a unified backend API.

A central component for this architecture is the APILauncher, located in Desktop/APILauncher. This .NET-compiled C# tool is responsible for initiating and managing the API server, which is built using Azure Functions Core Tools. The launcher first verifies the presence of the .NET runtime and installs it if necessary, then starts the API server and redirects its output to the console for monitoring.

The desktop application itself leverages .NET MAUI for cross-platform development. The core configuration resides in Desktop/MauiProgram.cs, which sets up the MAUI application, integrates BlazorWebView for rendering web content, and configures essential services like an HttpClient for network requests and IJSRuntime for JavaScript interop. Platform-specific entry points are defined in Desktop/Platforms, including MainActivity and MainApplication for Android, AppDelegate and Program for Mac Catalyst and iOS, and App for Windows, all delegating application creation to MauiProgram.CreateMauiApp.

For macOS, the Desktop/MacOS directory contains a dedicated launcher. This launcher, within Desktop/MacOS/Launcher2, uses SwiftUI to provide a user interface for displaying server output and controlling the Azure Functions CLI process. It manages the process lifecycle, including launching, restarting, and terminating the CLI, and handles Python dependencies for Azure Functions via scripts in Desktop/MacOS/Launcher2.app/Contents/Resources/api-build/Azure.Functions.Cli/tools/python/packapp/__main__.py. A PowerShell script, Desktop/MacOS/UpdateDMG.ps1, automates the modification of macOS Disk Image (DMG) files for deployment.

Similarly, the Windows counterpart in Desktop/Windows is a Windows Forms application. Its Form1 manages the external func.exe process, captures its output, and provides buttons for interaction, including a relaunch function. Resource management for the Windows application is handled in Desktop/Windows/Properties.

Automated build and publishing processes are critical to managing these cross-platform applications. The Desktop/MAUIAppPublisher.ps1 script automates building and publishing the .NET MAUI application for Windows, using dotnet publish and Inno Setup for installer creation. The Desktop/generate-all-os-executables.ps1 script further automates the download of .NET runtimes and Azure Functions CLI, building both the API server and Desktop API Runner executables for specific operating systems, with current focus on osx-x64.

The desktop application also includes a Blazor WebAssembly frontend within Desktop/wwwroot. This provides client-side functionality, styling (in Desktop/wwwroot/css), JavaScript interop (in Desktop/wwwroot/js), and features such as astrological data display and an AI chatbot.

General utility functions intended for the desktop application are planned to reside in Desktop/Code/DesktopTools.cs, and consistent versioning information is embedded at compile time via Desktop/ThisAssembly.cs.

Client-Side Blazor WebAssembly Framework and Interop
The Blazor WebAssembly application provides a client-side framework for the VedAstro project, enabling interactive web and desktop experiences. Its main entry point is the index.html file, which sets up the foundational HTML structure, loads essential stylesheets and JavaScript libraries, and integrates third-party services such as Facebook and Google for authentication and calendar access. This initial setup is crucial for preparing the environment before the Blazor application takes control of rendering the user interface.

The application leverages client-side scripting and styling extensively. Core application styles, including font definitions, link and button styles, and form validation feedback, are managed in Desktop/wwwroot/css/app.css and Website/wwwroot/css/app.css. These files also integrate the Bootstrap framework for UI components and a responsive grid, along with the Open Iconic icon set for iconography. External JavaScript libraries, many loaded from CDNs, provide diverse functionalities such as data tables (tabulator.min.js), advanced date/time parsing (luxon.min.js), chart rendering (chart.umd.min.js), and PDF generation (html2pdf.bundle.min.js).

A significant aspect of the framework is its JavaScript interop layer, which bridges .NET and JavaScript functionalities. The window.Interop object, defined primarily in Desktop/wwwroot/js/Interop.js and Website/wwwroot/js/Interop.js, exposes a range of JavaScript functions that Blazor components can invoke. These functions facilitate:

DOM Manipulation: Dynamically adding/removing classes, setting text content, and managing element visibility.
Local Storage Management: Storing and retrieving data from the browser's local storage.
Search Functionality: Integrating fuzzy search capabilities using libraries like Fuse.
UI Interaction: Handling accordions, smooth scrolling, and other interactive elements.
File Operations: Saving generated files and converting HTML content to PDF.
Data Visualization: Drawing charts for astrological data.
External Integrations: Connecting with Google Calendar and other web services.
The Desktop/wwwroot/js/VedAstro.js and Website/wwwroot/js/VedAstro.js files further expand this interop, managing dynamic HTML tables for astrological data, generating Ashtakavarga charts, and implementing a WebSocket-based AI chatbot. These files also set global application settings, ensuring consistent behavior across the application. For further details on the desktop application's lifecycle management, refer to Desktop Application Architecture and API Management. Additionally, Desktop/wwwroot/js/URLS.js and Website/wwwroot/js/URLS.js dynamically manage API and web domain endpoints based on the deployment environment, ensuring correct routing for API calls. The raw assets for the desktop application, including AboutAssets.txt, are managed in Desktop/Resources/Raw, with instructions on inclusion and programmatic access provided within that directory.

Mobile Website Frontend Development
The mobile-optimized website frontend, located in the Website_Mobile directory, serves as the primary user interface for the VedAstro project. It provides a rich, interactive experience across various astrological tools and informational pages, with a focus on responsive design and dynamic content.

The architecture of the mobile frontend is component-based, where common UI elements such as sidebars, navigation bars, headers, and footers are dynamically injected or managed by client-side JavaScript. This modular approach facilitates reusability and maintainability of UI elements across different pages. For instance, placeholders like DesktopSidebarHolder, PageTopNavbar, PageHeader, and PageFooter are consistently present in HTML files such as Website_Mobile/Home.html and Website_Mobile/About.html, indicating that their content is populated by JavaScript. Aggressive cache control meta tags are frequently employed across HTML files (e.g., Website_Mobile/404.html, Website_Mobile/APIBuilder.html) to ensure that users always receive the latest version of the application, particularly relevant during active development.

Client-side scripting, primarily managed through JavaScript files in Website_Mobile/js, orchestrates dynamic content, user authentication, and data management. Core application logic and global utilities are encapsulated in Website_Mobile/js/VedAstro.js, which handles aspects like user identification, caching mechanisms for person lists (getPersonListFromCache), API interaction (GetAPIPayload), and device detection (IsMobile). Essential client-side functionalities, including error logging (handleError), UI animations (smoothSlideToggle), navigation history (updateHistory), and console messages (printConsoleMessage), are implemented in Website_Mobile/js/app.js. Specific functionalities for each page, such as OnClickCalculate for astrological computations or OnGoogleSignInSuccessHandler for authentication, are handled by dedicated JavaScript files like Website_Mobile/js/BirthTimeFinder.js and Website_Mobile/js/Login.js, respectively.

The frontend offers a diverse set of astrological tools. For example, the Website_Mobile/APIBuilder.html page provides an interface to configure and interact with an astrological data API, while Website_Mobile/BirthTimeFinder.html helps users determine forgotten birth times using methods like 'Constellation Animal' or 'Rising Sign'. Website_Mobile/GoodTimeFinder.html enables the identification of auspicious times for events, and Website_Mobile/Horoscope.html facilitates the generation and viewing of detailed horoscopes, including planetary positions and predictions. Compatibility reports for two individuals are generated via Website_Mobile/MatchChecker.html, and Website_Mobile/LifePredictor.html displays astrological life predictions visually. User authentication is a key feature, with Website_Mobile/Login.html supporting sign-in via Facebook and Google accounts, and Website_Mobile/RegisterSubscription.html managing user subscription registrations.

Data management involves managing person profiles, such as adding new profiles through Website_Mobile/AddPerson.html and editing existing ones via Website_Mobile/EditPerson.html. The list of saved profiles is displayed using the Website_Mobile/PersonList.html page. The website also incorporates informational and utility pages like Website_Mobile/About.html, Website_Mobile/ContactUs.html, and Website_Mobile/PrivacyPolicy.html. Static assets and various HTML pages, including API documentation, console greetings, and event charts, are stored in the Website_Mobile/data directory. For instance, Website_Mobile/data/EventsChartViewer.html is an interactive page designed to render astrological event charts optimized for mobile.

To enhance UI/UX, the frontend extensively utilizes third-party libraries. Styling is primarily driven by Bootstrap (Website_Mobile/css/third-party/bootstrap.min.css), providing a responsive design framework. Interactive elements and visual feedback are delivered through libraries like SweetAlert2 (Website_Mobile/css/third-party/sweetalert2.min.css) for alerts, Tippy.js (Website_Mobile/css/third-party/tippy.min.css) for tooltips, and Vanilla Calendar (Website_Mobile/css/third-party/vanilla-calendar.min.css) for date picking. Charting capabilities are provided by Chart.js (Website_Mobile/js/third-party/chart.umd.min.js), and icons are rendered using Iconify Icon (Website_Mobile/js/third-party/iconify-icon.min.js). These libraries are typically loaded at the end of the <body> tag to improve perceived page load performance, with custom JavaScript files using the defer attribute to ensure execution after HTML parsing.

Machine Learning and Data Pipelines
The project incorporates several components for machine learning and data processing, focusing on astrological calculations, including data generation, classification, and GPU acceleration for matching. It also integrates with the Hugging Face Hub for managing Vedic astrology planetary data and provides tools for extracting text and generating embeddings from PDF documents.

For astrological matching, the system employs a machine learning pipeline detailed in Matchmaking Machine Learning Pipeline. This pipeline leverages a DatasetFactory in MatchMLPipeline/DatasetFactory.cs to generate structured datasets from information about individuals, such as marriage and body characteristics. This factory utilizes large language models (LLMs) to extract information from person profiles, which are stored in Azure Table Storage. The core classification component is the NearestCentroidClassifier in MatchMLPipeline/NearestCentroidClassifier.cs, which is used for multi-class classification tasks. The system can also utilize GPU acceleration for computationally intensive mathematical operations, as demonstrated in MatchMLPipeline/Program.cs, particularly for matrix operations that benefit from parallel processing.

The project also manages extensive Vedic astrology planetary data through integration with the Hugging Face Hub, as outlined in Hugging Face Hub Integration for Planetary Data. Scripts like HuggingFace/pull.py and HuggingFace/push.py facilitate the downloading, local storage, and uploading of datasets, such as all-planet-data-london. This data is structured for question-answering tasks and provides comprehensive planetary information across various timeframes.

Additionally, the project includes functionalities for processing unstructured text data, specifically PDF documents. The DocToEmbeddings component, described in PDF Text Extraction and Hierarchical Chunking, handles text extraction from PDFs using tools like PdfReader. The extracted text is then organized into a hierarchical tree of Chunk objects, as implemented in DocToEmbeddings/Program.cs. This chunking mechanism segments large texts into manageable units, which are then prepared for the generation of text embeddings, although the embedding generation itself is currently designed as a stub for future integration with LLM-based embedding services.

Matchmaking Machine Learning Pipeline
The matchmaking machine learning pipeline is designed to generate, classify, and analyze data primarily related to personal information for astrological compatibility. This pipeline utilizes a DatasetFactory for data preparation, a NearestCentroidClassifier for classification tasks, and integrates GPU acceleration for mathematical operations to enhance performance.

The DatasetFactory in MatchMLPipeline/DatasetFactory.cs is central to generating various datasets, including marriage, body, and name embedding information, often derived from large language models (LLMs). This factory processes PersonListEntity objects, converting unstructured information into structured JSON and storing it in Azure Table Storage. It also generates numerical embeddings for person names, facilitating fuzzy name matching through cosine similarity. For instance, GeneratePersonLifeDataset orchestrates the parallel extraction of marriage and body information, while FillPersonNameEmbeddings creates the necessary embeddings for efficient person search.

The NearestCentroidClassifier in MatchMLPipeline/NearestCentroidClassifier.cs implements a multi-class classification model. It operates by calculating a centroid (mean) for each class from training data and then classifying new data points based on their Euclidean distance to these centroids. The classifier supports training, predicting labels, estimating probabilities, and evaluating model performance using metrics like accuracy and confusion matrices. The demonstration in MatchMLPipeline/Program.cs showcases loading a pre-trained model and performing predictions, indicating its application in areas such as marriage prediction. Sample datasets for training and testing, such as MatchMLPipeline/Data/penguin_train_30.txt and MatchMLPipeline/Data/penguin_test_10.txt, are also part of this pipeline.

For computationally intensive mathematical operations, the pipeline integrates GPU acceleration using the ILGPU library. This is demonstrated in the Main5 method within MatchMLPipeline/Program.cs, which illustrates how parallel computations, such as Abs, Clamp, and Min, can be offloaded to a GPU. This capability is used for performing calculations on ArrayViews, managing GPU memory, and retrieving results, thereby optimizing performance for specific mathematical tasks within the machine learning workflow.

Hugging Face Hub Integration for Planetary Data
The project integrates with the Hugging Face Hub to manage and share Vedic astrology planetary data, primarily for use in question-answering tasks. This integration facilitates the storage, retrieval, and publishing of comprehensive planetary information across extensive time spans.

The data management process involves both downloading and uploading datasets. The script in HuggingFace/pull.py is responsible for downloading specified datasets from the Hugging Face Hub, such as vedastro-org/all-planet-data-london, and saving them to a local directory. This ensures that the application can access up-to-date planetary data for its calculations and predictions. Conversely, HuggingFace/push.py handles the uploading of local CSV data, specifically ml-table.csv, to a designated Hugging Face Hub repository, "vedastro-org/all-planet-data-london". This mechanism allows for the contribution and sharing of internally generated or updated planetary datasets. The dataset, generated using an ML Table Generator, is described in HuggingFace/README.md.

PDF Text Extraction and Hierarchical Chunking
The system provides functionality for extracting text from PDF documents and organizing it into a hierarchical structure. This process involves converting the raw text from PDFs into a tree of Chunk objects, designed to prepare the content for subsequent operations such as generating text embeddings.

The text extraction component reads PDF files, concatenating text from all pages into a single string. This raw text is then processed by a recursive chunking algorithm, which breaks down large text segments into smaller Chunk objects. This hierarchy is built by attempting to split text at natural breakpoints like spaces to maintain word integrity, with configurable parameters controlling the maximum size of a parent chunk and the smallest allowable chunk size. Each Chunk object holds a segment of text and includes a property intended for storing generated embeddings, although the actual integration for embedding generation is currently represented by placeholder values.

Utility and Automation Tools
The project incorporates various standalone tools designed to streamline astrological calculations, leverage large language models (LLMs) for coding assistance, manage geographical data, scrape external information, and automate code generation.

For astrological calculations, a console application located in Console is dedicated to finding optimal birth times and generating detailed event charts. This application, primarily defined in Console/Program.cs, allows users to input personal details and time ranges to visualize astrological events. It utilizes predefined Ayanamsa, EventTags, and AlgorithmFuncs to generate EventsCharts, which are then compiled into a single SVG file for output.

For developers, the LLM-powered coding assistant, LLMCoder, provides an interface for interacting with various LLMs. This Windows Forms application, detailed in LLMCoder, allows users to configure LLM parameters, inject code snippets or entire files with specific line ranges, and manage chat history. Key functionalities are handled by the Form1 class, found in LLMCoder/Form1.cs, which orchestrates UI interactions, conversation state, and communication with LLMs via SendMessageToLLM. The LLMCoder tracks token and kilobyte usage and supports preset management for code file injections.

Data management is supported by utilities within MigrateGeoLocationData, which focuses on migrating geolocation and timezone data. The MigrateGeoLocationData/Program.cs and MigrateGeoLocationData/ProgramTimezone.cs files in this directory facilitate reading geographical information from CSV files, processing it, and populating a database with GeoLocationTimezoneEntity records. These tools use Parallel.ForEachAsync to efficiently handle data migration and interact with a LocationManager to ensure data consistency. Additionally, MigrateGeoLocationData/ProgramCleanPersonList.cs provides a utility to delete specific entries from an Azure Table Storage PersonList based on a partition key and timestamp range.

External astrological data is gathered using web scraping tools located in WebScraper. The AstroSeekWebScraper class, defined in WebScraper/AstroSeekWebScraper.py, is designed to extract birth information, gender, and location for famous individuals from Astro-Seek.com. It employs requests for HTTP communication, BeautifulSoup for HTML parsing, and concurrent.futures.ThreadPoolExecutor for parallel processing. The collected data can then be added to the VedAstro database via a local API. The orchestration of this scraping process is managed by WebScraper/WebScraper.py.

Finally, the project includes automation for static code artifact generation, primarily managed by StaticTableGenerator. The StaticTableGenerator/Program.cs file in this directory automates the creation of OpenAPI method metadata, Python API stubs, and static data tables for event and horoscope data. It uses Microsoft.CodeAnalysis.CSharp (Roslyn) to parse C# source code, extracting XML documentation to generate OpenAPIMetadata. This tool also processes XML files (EventDataList.xml and HoroscopeDataList.xml) to create static C# classes and enumerations, and integrates with an external AI service for text summarization with caching to avoid redundant calls.

Astrological Console Application for Optimal Birth Times
The console application in Console provides an interactive command-line interface for advanced astrological calculations. Its primary function is to assist in finding optimal birth times and generating detailed event charts. Users navigate a menu to select specific tasks, such as generating "Life Predictor" charts for individuals.

The core of the application's functionality lies in its ability to generate EventsChart objects, which represent astrological events over a given time range. To achieve this, it retrieves person data and applies various astrological algorithms, such as Algorithm.General or Algorithm.IshtaKashtaPhalaDegree, based on specified EventTags. The application can iterate through a list of possible birth times, adjust a person's birth details accordingly, and then generate an EventsChart for each adjusted time.

The visual output of these calculations is rendered in SVG format. The application combines multiple individual chart SVGs into a single, comprehensive SVG file, dynamically positioning them to present a clear overview. This combined SVG file is then saved locally to the user's desktop. Configuration details, such as application secrets, are managed using Microsoft.Extensions.Configuration for secure handling.

LLM-Powered Coding Assistant (LLMCoder)
The LLM-Powered Coding Assistant (LLMCoder) is a Windows Forms application designed to streamline interactions with Large Language Models (LLMs) for coding assistance. Its user interface (UI), primarily defined in LLMCoder/Form1.Designer.cs, allows for managing LLM configurations, injecting various forms of code into prompts, and maintaining a clear history of conversations.

At its core, the application facilitates communication with different LLMs, such as GPT-4, Phi3, MistralNemo, MetaLlama, and Cohere. Users can select an LLM from a dropdown menu, and the application loads the corresponding API configurations, including endpoints and keys, from a secrets.json file. This allows for flexible switching between different language models as needed.

A key feature of LLMCoder is its ability to inject code into prompts. Users can either provide a large code snippet directly within a dedicated text box or inject entire code files or specific line ranges from files. When injecting files, the application dynamically generates UI components for each selected file, enabling users to define start and end line numbers, along with pre- and post-prompts to provide context around the injected code. This process is managed by AddNewFileInjectToVisibleList in LLMCoder/Form1.cs. The application compiles these various injected code elements, along with the ongoing conversation history, into a unified prompt before sending it to the selected LLM.

To assist with managing LLM interactions, LLMCoder tracks the estimated token and kilobyte usage of the conversation and injected code. This information is displayed in the UI, and a progress bar dynamically changes color to indicate proximity to the LLM's context window limit. This helps users manage the length of their prompts to avoid exceeding model limitations.

The application also maintains a persistent chat history, allowing users to review past interactions. This history can be loaded and reused, and individual messages can be edited or deleted. Furthermore, the application supports saving and loading presets for code file injections, enabling users to quickly switch between different coding contexts or project setups without reconfiguring each file injection manually. Presets are managed through FileInjectPreset objects, which are serialized to presets.json for persistence.

Geolocation and Timezone Data Migration Tools
The project includes utilities for migrating and managing geographical location and timezone data. These tools primarily focus on ingesting location information, including associated timezones, from CSV files into the project's database. This process involves reading various geographical data points such as latitude, longitude, and timezone text from each row of a CSV. The migration leverages parallel processing to efficiently handle numerous records, constructing GeoLocation objects and DateTimeOffset values from the input data.

For each record, the system checks whether a corresponding timezone entry already exists in the database. If not, a new GeoLocationTimezoneEntity is created and added. These entities capture the geographical coordinates and the precise timezone text, along with metadata indicating the source of the data. This ensures that astrological calculations can rely on accurate and comprehensive timezone information. The core logic for this migration is found in MigrateGeoLocationData/Program.cs and MigrateGeoLocationData/ProgramTimezone.cs.

Additionally, the suite includes a utility to clean up specific entries within the Azure Table Storage PersonList. This tool allows for the removal of records based on a specified partition key and a timestamp range, providing a mechanism for data hygiene and management within the storage system. This functionality is implemented in MigrateGeoLocationData/ProgramCleanPersonList.cs.

Astrological Data Web Scraper
The project includes web scraping tools designed to extract astrological data for famous individuals from Astro-Seek.com. This process identifies target profiles, parses HTML content to obtain birth information, gender, and location, and then integrates this data into the VedAstro database via a local API.

The core functionality for web scraping is encapsulated within the AstroSeekWebScraper class, located in WebScraper/AstroSeekWebScraper.py. This class manages the initiation of scraping, extraction of profile data, and persistence of the collected information. It supports parallel processing of famous individuals with "AA" (Accurate Ascendant) Rodden ratings to enhance efficiency. The scraper navigates through pages, extracts profile links, and then processes each link to retrieve detailed astrological data such as name, gender, birth time, and location. Date formats are converted to ensure consistency with the VedAstro system.

HTTP requests are handled using the requests library, and HTML parsing is performed with BeautifulSoup. Regular expressions assist in pattern matching for URLs and data extraction. The add_new_person_to_vedastro function interacts with a local VedAstro API endpoint to store the scraped data, suggesting an integration with a local development or deployment environment.

The entry point for the web scraping operation is defined in WebScraper/WebScraper.py. This file initializes the AstroSeekWebScraper with the target base URL and triggers the data collection process, separating the orchestration logic from the detailed scraping implementation for modularity.

Static Code Artifact Generation and Automation
The StaticTableGenerator component automates the creation of various static code artifacts, aiming to streamline development and reduce manual maintenance. This includes generating metadata for OpenAPI methods, creating C# classes for static data tables, and producing Python API stub files with type hinting. The process leverages Roslyn for C# source code analysis.

A key function of the generator is to extract comprehensive metadata from C# methods, particularly those designated as API calculators. This metadata encompasses method signatures, parameter descriptions, and general summaries, which are then used to construct an OpenAPIStaticTable.cs file. This C# class provides a structured and accessible representation of all detected API calculators within the system. Concurrently, Python stub files, such as Library.pyi, are generated to mirror the C# API methods, offering type hinting and documentation for Python clients and ensuring compatibility across language boundaries.

The tool also automates the creation of C# static data tables. For instance, EventDataList.xml and HoroscopeDataList.xml are processed to generate EventDataListStatic.cs and HoroscopeDataListStatic.cs, respectively. These generated files contain static representations of event and horoscope data, embedding this information directly into the codebase.

Furthermore, the StaticTableGenerator integrates an AI summarization feature. This functionality is used to generate concise descriptions for prediction texts, which are then incorporated into the static event data. To optimize resource usage and performance, this AI integration includes a caching mechanism that stores previously summarized texts, preventing redundant calls to the external AI service. The StaticTableGenerator is primarily managed through StaticTableGenerator/Program.cs.

Deployment and Publishing
The project automates the deployment of web assets to Azure Blob Storage and Azure CDN. This automation ensures that updated application files are synchronized, appropriately cached, and consistently delivered to users. The process involves synchronizing local files, injecting file hashes into client-side JavaScript, and subsequently purging the CDN cache.

The deployment system, primarily managed by the code in Publisher, handles the entire lifecycle from local file changes to live updates. It uses configuration data from secrets.json to establish connections with Azure services.

For client-side performance and cache invalidation, the system generates a SHA256 hash for specific JavaScript files, such as js/VedAstro.js. This hash is then injected into another JavaScript file, js/app.js, as a constant. This mechanism allows the client-side application to be aware of the current version of its JavaScript dependencies, facilitating efficient cache busting when updates occur.

File synchronization to Azure Blob Storage is managed by recursively scanning local project folders. To optimize this process, concurrent file uploads are limited, and files are only uploaded if they are new or have been modified since their last upload, determined by comparing timestamps. The system also automatically assigns the correct content type to each file based on its extension, ensuring proper handling by web browsers.

Finally, after files are updated in Blob Storage, the system initiates a purge of the Azure CDN cache for the affected files. This step is critical to ensure that end-users immediately receive the latest versions of the application, bypassing any stale content that might be cached at the CDN edge locations. This process leverages the Azure CLI for CDN cache invalidation.

Automated File Synchronization to Azure Blob Storage
The process of deploying web application assets to Azure Blob Storage involves synchronizing local project files with a specified container in the cloud. This synchronization is designed to be efficient, ensuring that only new or modified files are uploaded.

The system recursively scans a local directory, identifying files to be transferred. During this scan, certain project-related files and directories, such as solution files (.sln), project files (.csproj), and build artifacts (\bin\, \Properties\, \obj\), are excluded from the upload process.

For each file selected for upload, its ContentType is automatically determined based on its file extension. For instance, .html files are assigned text/html, and .js files receive application/javascript. This content type assignment is crucial for correct serving by web browsers and CDNs.

To optimize the upload process and conserve resources, the system employs a concurrency limit, allowing a maximum of 10 parallel file uploads. This ensures that the network and storage services are not overwhelmed during large deployments.

Furthermore, files are only uploaded if they are new to the Azure Blob Storage container or if the local file's last modification timestamp is more recent than the corresponding blob's LastModified timestamp. This timestamp-based comparison prevents unnecessary re-uploads of unchanged files, significantly reducing transfer times and costs.

Once files are synchronized, the system can proceed with cache invalidation on the Azure CDN to ensure that users receive the latest versions of the deployed application files. This process is further detailed in JavaScript File Hashing and Cache Invalidation.

JavaScript File Hashing and Cache Invalidation
To ensure that users consistently receive the most current version of the web application, a mechanism for JavaScript file hashing and cache invalidation is employed during the deployment process. This involves generating a SHA256 hash for the core JavaScript file, js/VedAstro.js, which encapsulates significant application logic. This generated hash is then programmatically injected into another client-side script, js/app.js, establishing a version control reference within the client-side code. This ensures that the application can internally track and verify the loaded version of its critical components.

Following the deployment and updating of files to Azure Blob Storage, the system performs a crucial step: purging the Azure CDN cache. By iterating through all updated files and issuing Azure CLI commands to purge their respective CDN endpoints, this process forces content delivery networks to retrieve the newly deployed files from Azure Blob Storage instead of serving stale cached versions. This two-pronged approach—hashing for internal version awareness and CDN purging for immediate content refreshment—guarantees that end-users consistently interact with the latest application updates, minimizing issues related to cached older versions. This process is orchestrated by the Program class, as detailed in Publisher/Program.cs. For further details on the overall deployment automation, refer to Automated File Synchronization to Azure Blob Storage.