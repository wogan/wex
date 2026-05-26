# Summary

Your task is to build an application that supports the requirements outlined below. Apart from
those requirements and the language specified in the Technical Implementation section below,
the application is your own design from a technical perspective.

This is your opportunity to show us what you know. Have fun, explore new ideas, and if you
have any questions about the requirements, please reach out as noted in the Questions section
below.

## Technical Implementation
The technical implementation (frameworks, libraries, etc.) is your own design, except the
application must be written in C#.

You should build this application as if it will be deployed to a production environment. This
means functional automated tests you would include for a production application are
expected.

Non-functional test automation (e.g., performance testing) is not required.

You are welcome to use Docker and any database technology you deem appropriate for a
production-ready C# application. The expectation is that the reviewer will have the .NET SDK
installed and should only need Docker in addition to that to run the application, without requiring
any other local tooling (e.g., a specific database engine).

## Requirements

### Requirement #1: Create a Card
Your application must be able to accept and store a Card with a credit limit. When a card is
stored, it will be assigned a unique identifier.

### Requirement #2: Store a Purchase Transaction
Your application must be able to accept and store a transaction associated with a specific card.

A transaction includes a description, transaction date, and an amount.

### Requirement #3: Retrieve a Purchase Transaction in a specified currency
Based on transactions previously submitted and stored, your application must provide a way to
retrieve stored transactions converted to currencies supported by the Treasury Reporting
Rates of Exchange API, based upon the exchange rate active for the date of the purchase.

https://fiscaldata.treasury.gov/datasets/treasury-reporting-rates-exchange/treasury-reporting-rates-of-exchange

The retrieved transaction should include the identifier, description, transaction date, the original
amount, the exchange rate used, and the converted amount in the specified currency.

#### Currency conversion requirements:
* When converting between currencies, you do not need an exact date match, but you
must use a currency conversion rate dated on or before the transaction date from within
the prior 6 months.
* If no currency conversion rate is available within 6 months on or before the transaction
date, an error should be returned stating the transaction cannot be converted to the
target currency.

### Requirement #4: Retrieve the Available Balance of a Card in a specified currency
Provide a way to retrieve a card’s available balance. The available balance is calculated as the
card’s credit limit minus the total of all transactions recorded for that card.

#### Currency conversion requirements:
* When converting to another currency, use the latest available exchange rate for that currency
from the Treasury Reporting Rates of Exchange API.

## Requirements Questions

Questions may be submitted via replying to all to the product brief email sent to you.

## Submission and Due Date

Submit your project via replying to all to the product brief email sent to you. Please include a link
to the public GitHub repository.

* You will have five (5) business days (Monday–Friday, excluding holidays) to complete
the exercise.
* We recommend spending no more than four (4) hours on the exercise.
* You are not expected to complete every requirement. Focus on writing clear,
maintainable code and demonstrating sound engineering practices.
* You may choose to use an AI-assisted tool to help you code more efficiently
