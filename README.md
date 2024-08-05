[![.NET](https://github.com/ErzenXz/news-aggregation/actions/workflows/dotnet.yml/badge.svg)](https://github.com/ErzenXz/news-aggregation/actions/workflows/dotnet.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=ErzenXz_news-aggregation&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=ErzenXz_news-aggregation)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=ErzenXz_news-aggregation&metric=bugs)](https://sonarcloud.io/summary/new_code?id=ErzenXz_news-aggregation)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=ErzenXz_news-aggregation&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=ErzenXz_news-aggregation)
[![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=ErzenXz_news-aggregation&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=ErzenXz_news-aggregation)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=ErzenXz_news-aggregation&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=ErzenXz_news-aggregation)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=ErzenXz_news-aggregation&metric=sqale_index)](https://sonarcloud.io/summary/new_code?id=ErzenXz_news-aggregation)
[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=ErzenXz_news-aggregation&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=ErzenXz_news-aggregation)


# News Aggregation Backend

The backend of the News Aggregation project handles the core functionalities and data management for aggregating, categorizing, and providing 
personalized news content. This document provides an overview of the backend requirements, setup instructions, and usage guidelines.

## Table of Contents

1. [General Requirements](#general-requirements)
2. [Backend Requirements](#backend-requirements)
3. [Installation](#installation)
4. [Usage](#usage)
5. [API Documentation](#api-documentation)
6. [Contributing](#contributing)
7. [License](#license)
8. [Contact](#contact)

## General Requirements

- **News Sources Integration**: Aggregate news from multiple sources.
- **Category Management**: Categorize news articles into different sections.
- **Personalized Feed**: Provide a personalized news feed based on user preferences.
- **Article Bookmarking**: Allow users to bookmark articles for later reading.
- **Commenting System**: Implement a commenting system for articles.
- **Notifications**: Send notifications for breaking news.
- **Search Functionality**: Advanced search for news articles.
- **User Profile Management**: Manage user preferences and saved articles.
- **Trending News**: Display trending news articles.
- **Ads Management**: Integrate ads within news articles.
- **Social Sharing**: Allow users to share articles on social media.
- **Multimedia Content**: Support for images, videos, and audio within articles.
- **Content Recommendations**: Recommend related articles based on reading history.
- **Content Moderation**: Tools for reporting and moderating comments.
- **Subscription Model**: Implement a subscription model for premium content.

## Backend Requirements

### Basic Requirements:

- **API Development**:
    - Develop RESTful APIs using ASP.NET.
    - Handle HTTP methods (GET, POST, PUT, DELETE) with synchronous and asynchronous support.
    - Validate user input and include appropriate error handling and logging.
- **Integration and Interoperability**:
    - Integrate APIs with PostgreSQL database.
    - Implement authentication and authorization mechanisms (JWT tokens, OAuth2 for Google, Discord, GitHub).
    - Document APIs using Swagger or OpenAPI.
    - Integrate with Stripe for payment processing.
- **Performance and Scalability**:
    - Use ElasticSearch for efficient data storage and indexing.
    - Integrate with Redis for caching.
    - Use RabbitMQ for asynchronous tasks.
    - Support real-time communication with WebSockets.
- **Deployment and Testing**:
    - Create a deployment plan for API availability.
    - Include unit and integration tests.
- **Security**:
    - Ensure secure data transmission and storage.
- **Documentation**:
    - Provide detailed API documentation using Swagger or OpenAPI.

### CRUD Requirements:

- **User Management**:
    - User authentication and authorization.
    - CRUD operations for user profiles and data.
- **Content Management**:
    - CRUD operations for entities like articles, categories, bookmarks, comments, user preferences, subscriptions, plans, payments, users, and sources.
    - Implement ElasticSearch for content search.
    - Use Redis for caching to improve performance.

### Advanced Requirements:

- **Real-time and Background Processing**:
    - Real-time updates with WebSockets.
    - Image and video hosting using AWS S3.
    - Push notifications using a third-party service.
    - Queueing system for background tasks.
- **Scalability and Optimization**:
    - Implement load balancing and horizontal scaling.
    - Optimize database queries and indexing for performance.
- **Monitoring and Logging**:
    - Implement logging for monitoring API usage and errors.
    - Set up monitoring tools to track API performance and health.

## Installation

### Prerequisites:

- .NET SDK
- PostgreSQL
- Redis
- RabbitMQ
- ElasticSearch
- AWS S3 (for multimedia content)
- Stripe account (for payment processing)

### Steps:

1. **Clone the repository**:

    ```bash
    git clone https://github.com/ErzenXz/news-aggregation.git
    cd NewsAggregation
    ```

2. **Install dependencies**:

    ```bash
    dotnet restore
    ```

3. **Setup environment variables**:

   Configure the following environment variables in your `.env` file:

    ```env
    ConnectionStrings__PostgreSQL=YourPostgreSQLConnectionString
    JWT__Secret=YourJWTSecret
    OAuth2__Google=YourGoogleOAuthClientID
    OAuth2__Discord=YourDiscordOAuthClientID
    OAuth2__GitHub=YourGitHubOAuthClientID
    Stripe__ApiKey=YourStripeApiKey
    ElasticSearch__Url=YourElasticSearchUrl
    Redis__Connection=YourRedisConnectionString
    RabbitMQ__Url=YourRabbitMQUri
    ```

4. **Run database migrations**:

    ```bash
   dotnet ef migrations add InitialCreate
    dotnet ef database update
    ```

5. **Start the application**:

    ```bash
    dotnet run
    ```

## Usage

- **API Endpoints**: Detailed API documentation is available at `/swagger` once the application is running.
- **Admin Panel**: Manage categories, articles, and ads via the admin panel available at `/admin`.
- **User Management**: Users can manage their profiles, bookmarks, and preferences through the user endpoints.

## API Documentation

API documentation is generated using Swagger. Visit `/swagger` after running the application to explore and test the API endpoints.

## Contributing

We welcome contributions! Follow these steps to contribute:

1. Fork the repository.
2. Create a new branch (`git checkout -b feature-branch`).
3. Make your changes.
4. Commit your changes (`git commit -m 'Add some feature'`).
5. Push to the branch (`git push origin feature-branch`).
6. Open a pull request.

## License

This project is licensed under the AGPL 3.0 License - see the [LICENSE](LICENSE) file for details.

