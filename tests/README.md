# System Prompt Switching GPT Bot - End-to-End Tests

This directory contains end-to-end (E2E) regression tests for the System Prompt Switching GPT Bot. These tests validate the core user journey and ensure that the bot's functionality remains intact across changes.

## Running the Tests

To run all tests:

```bash
cd tests
dotnet test
```

To run tests with detailed output:

```bash
dotnet test --logger "console;verbosity=detailed"
```

## Test Coverage

The E2E test suite (`GPTBotE2ETests.cs`) covers the following scenarios:

### 1. Mode Switching (`ModeSwitching_ShouldResetStateAndSwitchPrompt`)
- **Purpose**: Verifies that mode switching commands (e.g., `/translate`) work correctly
- **Validates**: 
  - Command is recognized
  - Confirmation message is sent
  - Bot switches to the new mode

### 2. Clear Command (`ClearCommand_ShouldResetConversationHistory`)
- **Purpose**: Ensures the `/clear` command resets conversation history
- **Validates**:
  - Clear command clears conversation state
  - Confirmation message is sent

### 3. Invalid Command (`InvalidCommand_ShouldReturnNotFoundMessage`)
- **Purpose**: Tests error handling for unknown commands
- **Validates**:
  - Invalid commands are detected
  - Appropriate error message is returned

### 4. Normal Conversation (`NormalConversation_ShouldCallOpenAIAndReply`)
- **Purpose**: Tests the basic chat functionality
- **Validates**:
  - User messages trigger OpenAI API calls
  - Responses are returned to the user
  - OpenAI client is invoked correctly

### 5. Conversation State Persistence (`ConversationState_ShouldPersistAcrossMessages`)
- **Purpose**: Verifies that conversation history is maintained across multiple messages
- **Validates**:
  - Multiple messages in sequence work correctly
  - State is persisted between turns

### 6. Mode Switch with Conversation (`ModeSwitch_ThenConversation_ShouldUseNewPrompt`)
- **Purpose**: Tests the complete flow of switching modes and then having a conversation
- **Validates**:
  - Mode can be switched
  - Subsequent conversations use the new system prompt
  - OpenAI is called with the correct prompt configuration

## Architecture

The tests use:
- **xUnit**: Testing framework
- **Moq**: Mocking framework for dependencies
- **Microsoft.Bot.Builder.Testing**: Bot Framework testing utilities
- **TestAdapter**: Simulates bot conversation flows

### Mocking Strategy

- **OpenAI Client**: Mocked to provide deterministic responses without calling the actual OpenAI API
- **State Management**: Uses in-memory storage for testing
- **Bot Responses**: Validated using TestFlow assertions

## Deterministic Testing

All tests are deterministic and do not require external dependencies:
- OpenAI API calls are mocked
- No network requests are made
- All state is stored in memory
- Tests can run in isolation

## Adding New Tests

When adding new functionality to the bot, add corresponding E2E tests to ensure:
1. The new feature works end-to-end
2. Existing functionality is not broken
3. The test is deterministic and repeatable

Follow the existing test patterns:
```csharp
[Fact]
public async Task YourTest_ShouldDoSomething()
{
    // Arrange
    var bot = CreateBot();
    var adapter = new TestAdapter();
    SetupMockOpenAIResponse("Expected response");

    // Act & Assert
    await new TestFlow(adapter, async (turnContext, cancellationToken) =>
    {
        await bot.OnTurnAsync(turnContext, cancellationToken);
    })
    .Send("User input")
    .AssertReply(activity => {
        // Assertions here
    })
    .StartTestAsync();
}
```

## Continuous Integration

These tests should be run as part of CI/CD pipelines to catch regressions early. They typically complete in under 1 second.
