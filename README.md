# PBT205 — Assessment 1: Prototype Development

**Torrens University Australia**  
**Subject:** PBT205 — Project-based Learning Studio: Technology  
**Group 3:** Tia Darvell · David Ristevski · Nicholas Beltran  
**Submission Date:** 29 March 2026  
**Weighting:** 45% · 100 Marks

---

## Overview

Three distributed software prototypes communicating via RabbitMQ middleware. Each prototype is a set of command-line applications and a real-time web GUI, built with C# .NET 10, SignalR, MassTransit, and Docker. A single RabbitMQ instance is shared across all three tasks.

| Task | Prototype | Port | Middleware Topics |
|------|-----------|------|-------------------|
| 1 | Chat Application | 5001 | `room.<roomname>` — Topic Exchange |
| 2 | XYZ Stock Exchange | 5219 | `orders`, `trades` — Fanout Exchange |
| 3 | Contact Tracing System | 5220 | `position`, `query`, `query-response` — Fanout Exchange |

---

## Project Structure

```
pbt205-Group3-Assessment1/
│
├── Task1-ChatApp/
│   ├── ChatApp/                  # CLI — multi-room chat client
│   │   ├── Consumers/
│   │   ├── Messages/
│   │   ├── Middleware/
│   │   └── Program.cs
│   ├── ChatGUI/                  # ASP.NET Core + SignalR web GUI
│   │   ├── Hubs/
│   │   ├── Services/
│   │   └── wwwroot/index.html
│   └── Dockerfile
│
├── Task2-XYZExchange/
│   ├── SendOrderApp/             # CLI — submits a single order and exits
│   ├── ExchangeApp/              # CLI — order matching engine
│   ├── TradingCore/              # Shared models and RabbitMQService
│   └── TradingGuiApp/            # ASP.NET Core + SignalR web GUI
│
├── Task3-ContactTracing/
│   ├── ContactTracingCore/       # Shared models and RabbitMQService
│   ├── TrackerApp/               # CLI — tracks positions and contacts
│   ├── PersonApp/                # CLI — random movement publisher
│   ├── QueryApp/                 # CLI — queries contacts, prints and exits
│   └── ContactTracerGui/         # ASP.NET Core + SignalR web GUI
│
├── dashboard.html                # Local dashboard linking all three GUIs
├── docker-compose.yml            # Shared RabbitMQ instance
└── README.md
```

---

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — for RabbitMQ
- [.NET 10 SDK](https://dotnet.microsoft.com/download) — for building and running all projects
- A terminal — VS Code integrated terminal recommended

---

## Getting Started

### 1. Clone the Repository

```bash
git clone <your-repo-url>
cd pbt205-Group3-Assessment1
```

### 2. Start RabbitMQ

All three tasks share a single RabbitMQ instance. **Start this first** before running any application:

```bash
docker compose up rabbitmq
```

Verify it is running at [http://localhost:15672](http://localhost:15672)  
Login: `guest` / `guest`

### 3. Open the Dashboard

Once all three GUIs are running (see below), open the dashboard to access all three from one place:

```bash
open dashboard.html
```

---

## Task 1 — Chat Application

A multi-room CLI chat application with a real-time web GUI. Uses a RabbitMQ **topic exchange** named `room` with routing keys `room.<roomname>`.

### Architecture
```
ChatApp CLI ──publish──▶ RabbitMQ (room topic) ──subscribe──▶ ChatApp CLI (other users)
                                                └──subscribe──▶ ChatGUI (SignalR → browser)
```

### Run the GUI
```bash
dotnet run --project Task1-ChatApp/ChatGUI
```
Open [http://localhost:5001](http://localhost:5001)

### Run the CLI
```bash
dotnet run --project Task1-ChatApp/ChatApp -- <username> <endpoint> <room>
```

**Example — two users chatting:**
```bash
# Terminal 1
dotnet run --project Task1-ChatApp/ChatApp -- Tia localhost general

# Terminal 2
dotnet run --project Task1-ChatApp/ChatApp -- David localhost general
```

**Available rooms:** `general` · `sports` · `lobby`

Messages appear in real time in the GUI and in all other CLI terminals in the same room.

---

## Task 2 — XYZ Stock Exchange

A distributed trading system with order matching and a live price feed GUI. Uses two RabbitMQ **fanout exchanges**: `orders` and `trades`.

### Architecture
```
SendOrderApp ──publish──▶ RabbitMQ (orders) ──subscribe──▶ ExchangeApp
                                                                │
                                                           match orders
                                                                │
TradingGuiApp ◀──subscribe── RabbitMQ (trades) ◀──publish──────┘
```

### Run the Exchange
```bash
dotnet run --project Task2-XYZExchange/ExchangeApp -- localhost
```

### Run the GUI
```bash
dotnet run --project Task2-XYZExchange/TradingGuiApp
```
Open [http://localhost:5219](http://localhost:5219)

### Submit Orders
```bash
dotnet run --project Task2-XYZExchange/SendOrderApp -- <username> <endpoint> <BUY|SELL> <quantity> <price>
```

**Example — matched trade:**
```bash
dotnet run --project Task2-XYZExchange/SendOrderApp -- Alice localhost BUY 100 10.50
dotnet run --project Task2-XYZExchange/SendOrderApp -- Bob localhost SELL 100 10.50
```

The exchange matches the orders and publishes the completed trade. The GUI updates in real time showing the latest price, buyer, seller, quantity, and trade history.

> Quantity is fixed at 100 shares per the assessment specification. A BUY and SELL order match when the buyer's price is greater than or equal to the seller's price.

---

## Task 3 — Contact Tracing System

A real-time 2D grid environment where people move randomly (King's movement) and contacts are recorded when two people occupy the same square simultaneously. Uses three RabbitMQ **fanout exchanges**: `position`, `query`, and `query-response`.

### Architecture
```
PersonApp ──publish──▶ RabbitMQ (position) ──subscribe──▶ TrackerApp (logs contacts)
                                            └──subscribe──▶ ContactTracerGui (live grid)

QueryApp ──publish──▶ RabbitMQ (query) ──subscribe──▶ TrackerApp
                                                           │
ContactTracerGui ◀──subscribe── RabbitMQ (query-response) ◀┘
```

### Run the Tracker (start first)
```bash
dotnet run --project Task3-ContactTracing/TrackerApp -- localhost
```

### Run the GUI
```bash
dotnet run --project Task3-ContactTracing/ContactTracerGui
```
Open [http://localhost:5220](http://localhost:5220)

### Start People
```bash
dotnet run --project Task3-ContactTracing/PersonApp -- <endpoint> <name> <moves-per-second>
```

**Example — two people on the board:**
```bash
# Terminal 1
dotnet run --project Task3-ContactTracing/PersonApp -- localhost Alice 1

# Terminal 2
dotnet run --project Task3-ContactTracing/PersonApp -- localhost Bob 1
```

### Query Contacts
```bash
dotnet run --project Task3-ContactTracing/QueryApp -- <endpoint> <name>
```

**Example:**
```bash
dotnet run --project Task3-ContactTracing/QueryApp -- localhost Alice
```

Returns all people Alice has contacted, in reverse-chronological order. Results appear in both the terminal and the GUI.

> Default board size is 10×10. Configurable up to 1000×1000 via the optional 4th argument to PersonApp: `PersonApp localhost Alice 1 50`

---

## Technology Stack

| Technology | Purpose |
|------------|---------|
| C# .NET 10 | All application logic |
| RabbitMQ 3.13 | Middleware — message broker |
| MassTransit 8.3 | RabbitMQ abstraction (Task 1 CLI) |
| RabbitMQ.Client 7.2 | Direct AMQP client (Tasks 2, 3, GUIs) |
| ASP.NET Core | Web server for all three GUIs |
| SignalR | Real-time browser updates |
| Newtonsoft.Json | JSON serialisation |
| Docker | RabbitMQ containerisation |

---

## Assumptions

| # | Assumption | Reason |
|---|------------|--------|
| 1 | A demonstration video is required for each task | Advised verbally during class; not in the written brief |
| 2 | Each video is a screen-recorded walkthrough covering all CLI and HD GUI requirements | No format guidance provided |
| 3 | Quantity is fixed at 100 shares per order for Task 2 | Specified in the brief |
| 4 | Person identifiers in Task 3 are unique first names | Permitted by the brief for simplicity |
| 5 | Movement speed in Task 3 is expressed as moves per second | Brief left format to team's discretion |

---

## Known Limitations

- **Task 2:** If multiple ExchangeApp instances are started, each will receive every order and attempt to match independently, resulting in duplicate trades. The spec states there can only be one exchange.
- **Task 2:** Subscriber queues are exclusive and auto-delete — orders published while ExchangeApp is not running are lost.
- **Task 3:** Board rendering in the GUI is optimised for 10×10. Very large boards would require a pan/zoom system.
- **Dashboard:** Status indicators poll localhost ports and only work when all three GUIs are running on the same machine.

---

## Team

| Member | Standing Role | Task 1 | Task 2 | Task 3 |
|--------|--------------|--------|--------|--------|
| Tia Darvell | Project Lead · Coordinator | Core App Logic | Middleware · Task Lead | GUI |
| David Ristevski | Documentation · Media Lead | Middleware · Task Lead | Core App Logic · GUI | — |
| Nicholas Beltran | Quality Assurance Lead | GUI | Code Review | Core App Logic · Middleware · Task Lead |

---

## License

MIT License — see [LICENSE](LICENSE) for details.
