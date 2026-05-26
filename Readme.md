# Wex take home coding exercise

See [Task.md](./Task.md) for the task definition.

## API Overview

Create a card:
```http request
POST http://localhost:5014/cards
Content-Type: application/json

{
"limitAmount": 500000
}
```

Retrieve a card:
```http request
GET http://localhost:5014/cards/1
```

Create a transaction:
```http request
POST http://localhost:5014/transactions
Content-Type: application/json

{
"cardId": 1,
"amount": "23.50",
"date": "2026-04-28T20:13:15.123Z",
"description": "This is a test transaction"  
}
```

Retrieve a transaction:
```http request
GET http://localhost:5014/transactions/1
```

Retrieve a transaction with country and currency parameters:
```http request
GET http://localhost:5014/transactions/1?country=Australia&currency=Dollar
```

Retrieve a card balance:
```http request
GET http://localhost:5014/cards/1/balance?country=Australia&currency=Dollar
```

## Project setup
* Ensure you have `dotnet` and `dotnet-ef` installed.
* Ensure you have docker installed.
* Run `docker-compose up` to start the database.
* Run `dotnet ef database update` to create the database schema.
* Run `dotnet run` to start the application.

## Assumptions
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

## Potential Improvements
* Caching of exchange rates as they don't change frequently.
* As more APIs are added, I suspect we would want to move to controllers rather than thin API endpoints.
  * Also additional service layers to handle business logic.

## Out of scope
* Authentication and authorization.
* Operational metrics (latency, errors, etc) and logging.
