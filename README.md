# Edison – Client–Server Vehicle Reservation System

A console client–server application where users publish vehicle models and reserve them over the network.

It is built with **C# / .NET 10** on **raw TCP sockets**, with a **custom application protocol** and **low-level concurrency using threads and locks**. The project deliberately avoids `Task`, `async`/`await` and high-level networking libraries.

> Academic project developed for *Network Programming* (Programación de Redes) at Universidad ORT Uruguay, 2026.

## Features

- **Accounts:** register, log in and log out.
- **Vehicle models:** publish, update and delete your own models (name, description, price, delivery date, stock). Only the creator can change or delete a model.
- **Search:** search vehicles, view model details and availability, and list your own models.
- **Reservations:** request a reservation, check its status and cancel it. Reservations are resolved asynchronously by the server.
- **Images:** upload and download a picture for each model, transferred in chunks over the socket.
- **Activity history:** each user can see a log of their actions, such as models published and reservations confirmed.

## Architecture

```
Edison.sln
├── Server/   Listener, one thread per client, request handlers, in-memory repositories, reservation scheduler
├── Client/   Console UI (menus) that sends requests and shows responses
└── Common/   Shared code: protocol, socket helper, file transfer, models, configuration
```

```
 Client                                   Server
┌────────────┐   TCP (port 20000)   ┌───────────────────────────────┐
│ Console UI │ ───────────────────▶ │ Listener thread               │
│            │ ◀─────────────────── │   └─ one thread per client    │
└────────────┘   request/response   │        └─ handlers ─▶ repos   │
                                    │ Background threads:           │
                                    │   reservation resolution      │
                                    └───────────────────────────────┘
```

## Application protocol

TCP delivers a continuous stream of bytes with no message boundaries, so the client and server agree on a frame format to know where each message starts and ends.

Every message is a frame with a fixed 6-byte header followed by the payload:

| Field | Size | Example | Meaning |
|---|---|---|---|
| CMD | 2 bytes (ASCII) | `07` | Operation number, the same in the request and its response |
| LENGTH | 4 bytes (ASCII) | `0012` | Size of the payload in bytes (maximum 9999) |
| DATA | variable | `0003ana0004Volt` | Operation content |

- The receiver first reads the 6 header bytes, then reads exactly `LENGTH` more bytes.
- Sends and receives loop until every byte is transferred, because a single socket call may move only part of the buffer.
- Inside `DATA`, each field is prefixed by its own 4-digit length. For example, `ana` is sent as `0003ana`.
- Responses include a status code modeled on HTTP: `200`, `400`, `401`, `403`, `404`, `409` and `500`.
- The protocol defines 16 operations, from `Register = 1` to `ListVehiclesWithImage = 16`.

### File transfer

Images can be larger than a single frame, so they are split into chunks of up to 9,000 bytes. Each chunk carries its offset and the total file size. The server writes chunks to disk with `FileStream` as they arrive, and serves downloads the same way, reading only the requested range of the file.

## Concurrency

The course required `Thread` as the concurrency mechanism, so all concurrency is managed by hand:

- **Listener thread:** accepts incoming connections.
- **One thread per client:** each connected client is served by its own thread for the lifetime of the session.
- **Reservation threads:** a reservation stays `Pending` and is resolved by a background thread after a configurable delay. It becomes `Confirmed` if there is stock available at that moment, or `Rejected` otherwise. Stock is checked again on resolution, because the model may have changed while waiting.
- **Thread-safe repositories:** shared in-memory data is protected with `lock`, so concurrent clients cannot corrupt it or reserve the same unit twice.
- **Graceful shutdown:** typing `exit` in the server console closes the listener, disconnects clients and waits for their threads to finish.

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Run the server

```bash
dotnet run --project Server
```

Type `exit` in the server console to shut it down.

### Run one or more clients

In another terminal (open several to test concurrency):

```bash
dotnet run --project Client
```

### Sample data

With `LoadSampleData` enabled, the server starts with three users, `ana`, `luis` and `sofia` (password `1234`), plus sample vehicles and reservations.

## Configuration

Settings live in each project's `App.config`:

| Setting | Default | Description |
|---|---|---|
| `ServerIp` / `ServerPort` | `127.0.0.1` / `20000` | Address the server listens on and the client connects to |
| `ReservationProcessingSeconds` | `10` | Delay before a pending reservation is resolved |
| `ImagesDirectory` | `Images` | Folder where uploaded images are stored |
| `LoadSampleData` | `true` | Load sample users, vehicles and reservations on startup |

Data is kept in memory and resets when the server restarts.

## Authors

- Federica Paz Vitale – [GitHub](https://github.com/YOUR-USERNAME) · [LinkedIn](https://linkedin.com/in/federica-paz-vitale)
- Sasha Yeye
