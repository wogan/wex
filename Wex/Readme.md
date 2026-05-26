= Wex take home coding exercise =

== Project setup ==
* Ensure you have `dotnet` and `dotnet-ef` installed.
* Ensure you have docker installed.
* Run `docker-compose up` to start the database.
* Run `dotnet ef database update` to create the database schema.
* Run `dotnet run` to start the application.

== Assumptions ==
* Card limits and transactions amounts are stored in USD.
* We don't need to block transactions that cause a card to exceed its limit – reporting a negative balance is fine.
* 255 characters is sufficient for transaction descriptions.
* A known set of currencies is not required, the application supports any currency/country pair that the Exchange rate
  API supports.
  * We could simplify the API surface by having a hardcoded list of currencies that map to the currency/country pairs
    that the Exchange rate API supports.
* Currency conversion does not need to "round to the nearest two decimal places" but can return fractional values.
* Database IDs don't need to be obfuscated for the client (e.g., with UUIDs or similar). In a production system
  I would expect we don't expose the database IDs to the client.

== Potential Improvements ==
* Caching of exchange rates as they don't change frequently.
* As more APIs are added, I suspect we would want to move to controllers rather than thin API endpoints.
  * Also additional service layers to handle business logic.

== Out of scope ==
* Authentication and authorization.
* Operational metrics (latency, errors, etc) and logging.
