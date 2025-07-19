# MarketMagic ZMQ Server API Documentation

## Overview
ZMQ Server poskytuje REQ-REP komunikačný vzor na porte 5555 (predvolene). Všetky správy sú vo formáte JSON.

## Spustenie servera
```bash
julia server.jl [port]
```

## API Commands

### 1. loadUploadTemplate
Načíta upload template zo súboru.

**Request:**
```json
{
    "command": "loadUploadTemplate",
    "path": "./data/template.csv"
}
```

**Response (Success):**
```json
{
    "success": true,
    "message": "Upload template loaded successfully from: ./data/template.csv"
}
```

**Response (Error):**
```json
{
    "success": false,
    "error": "Failed to load upload template: <error_details>"
}
```

### 2. addExportedData
Pridá exportované dáta k aktuálnemu upload template.

**Request:**
```json
{
    "command": "addExportedData",
    "path": "./data/active.csv"
}
```

**Response (Success):**
```json
{
    "success": true,
    "message": "Exported data added successfully from: ./data/active.csv"
}
```

**Response (Error):**
```json
{
    "success": false,
    "error": "No upload template loaded. Load upload template first."
}
```

### 3. saveUploadTemplate
Uloží aktuálny upload template do súboru.

**Request:**
```json
{
    "command": "saveUploadTemplate",
    "path": "./data/output.csv"
}
```

**Response (Success):**
```json
{
    "success": true,
    "message": "Upload template saved successfully to: ./data/output.csv"
}
```

### 4. fetchUploadTemplate
Vráti aktuálny upload template dáta.

**Request:**
```json
{
    "command": "fetchUploadTemplate"
}
```

**Response (Success):**
```json
{
    "success": true,
    "data": {
        "id": 123456,
        "columns": ["*Title", "*StartPrice", "CustomLabel", "..."],
        "enums": {
            "C:Brand": ["Apple", "Samsung", "..."],
            "C:Color": ["Red", "Blue", "Green"],
            "...": []
        },
        "cells": [
            ["iPhone 13", "699.99", "SKU001", "..."],
            ["Samsung Galaxy", "599.99", "SKU002", "..."]
        ]
    }
}
```

## .NET Integration Notes

### Matrix Handling
Julia `Matrix{String}` sa konvertuje na `Vector{Vector{String}}` (array of arrays) pre JSON serialization. V .NET môžete použiť:

```csharp
// Ak použijete Newtonsoft.Json
public class UploadDataTable
{
    public long Id { get; set; }
    public string[] Columns { get; set; }
    public Dictionary<string, string[]> Enums { get; set; }
    public string[][] Cells { get; set; } // Array of arrays instead of matrix
}
```

### ZMQ .NET Library
Odporúčam použiť `NetMQ` balíček:
```
Install-Package NetMQ
```

### Example .NET Client Code
```csharp
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

public class MarketMagicClient
{
    private RequestSocket client;
    
    public void Connect(int port = 5555)
    {
        client = new RequestSocket();
        client.Connect($"tcp://localhost:{port}");
    }
    
    public UploadDataTable FetchUploadTemplate()
    {
        var request = new { command = "fetchUploadTemplate" };
        var requestJson = JsonConvert.SerializeObject(request);
        
        client.SendFrame(requestJson);
        var responseJson = client.ReceiveFrameString();
        
        var response = JsonConvert.DeserializeObject<ApiResponse<UploadDataTable>>(responseJson);
        
        if (response.Success)
        {
            return response.Data;
        }
        else
        {
            throw new Exception(response.Error);
        }
    }
    
    public void LoadUploadTemplate(string path)
    {
        var request = new { command = "loadUploadTemplate", path = path };
        var requestJson = JsonConvert.SerializeObject(request);
        
        client.SendFrame(requestJson);
        var responseJson = client.ReceiveFrameString();
        
        var response = JsonConvert.DeserializeObject<ApiResponse<object>>(responseJson);
        
        if (!response.Success)
        {
            throw new Exception(response.Error);
        }
    }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string Error { get; set; }
    public T Data { get; set; }
}
```

## Testing
Pre testovanie môžete použiť:
```bash
julia test_client.jl
```

## Error Handling
Všetky odpovede obsahujú `success` field. Pri chybe je `success: false` a `error` field obsahuje popis chyby.

## Server State
Server udržuje jeden globálny `uploadDataTable` objekt. Všetky operácie pracujú s týmto objektom.
