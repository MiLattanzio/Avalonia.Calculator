// Linux C ABI for the MIT-licensed Uno / Microsoft Calculator engine.
// All strings crossing this boundary are UTF-8 and owned by the engine instance.
#include "CalcManager/CalculatorManager.h"
#include "CalcManager/CalculatorResource.h"
#include "EngineResources.h"
#include "CalcManager/headers/CalcEngine.h"
#include "CalcManager/Ratpack/ratpak.h"
#include <codecvt>
#include <locale>
#include <memory>

using namespace CalculationManager;
static std::string utf8(const std::wstring& value) {
    return std::wstring_convert<std::codecvt_utf8<wchar_t>>().to_bytes(value);
}
static std::string quote(const std::wstring& value) {
    std::string out = "\"";
    for (unsigned char c : utf8(value)) {
        if (c == '"' || c == '\\') { out += '\\'; out += c; }
        else if (c < 32) { char hex[7]; snprintf(hex, sizeof(hex), "\\u%04x", c); out += hex; }
        else out += c;
    }
    return out + "\"";
}
struct Engine final : ICalcDisplay, IResourceProvider {
    std::wstring display = L"0", expression;
    std::vector<std::wstring> memory;
    bool error = false;
    int mode = 0;
    std::string buffer;
    std::map<std::wstring, std::wstring> resources = engineResources;
    void activate() { CCalcEngine::ReloadEngineStrings(*this); SetDecimalSeparator(resources.at(L"sDecimal")[0]); }
    // Destroy the manager first: its destructor notifies the display.
    std::unique_ptr<CalculatorManager> manager;
    Engine(const char* const* keys=nullptr, const char* const* values=nullptr, int count=0) {
        auto codec=std::wstring_convert<std::codecvt_utf8<wchar_t>>();
        for(int i=0;i<count;i++) resources[codec.from_bytes(keys[i])]=codec.from_bytes(values[i]);
        // RatPack initializes constants from invariant literals before SettingsChanged applies the locale.
        SetDecimalSeparator(L'.');
        manager = std::make_unique<CalculatorManager>(this, this); manager->Reset();
    }
    std::wstring GetCEngineString(const std::wstring& id) override {
        auto it = resources.find(id); return it == resources.end() ? id : it->second;
    }
    void SetPrimaryDisplay(const std::wstring& value, bool isError) override { display=value; error=isError; }
    void SetIsInError(bool value) override { error=value; }
    void SetExpressionDisplay(const std::shared_ptr<CalculatorVector<std::pair<std::wstring,int>>>& tokens,
        const std::shared_ptr<CalculatorVector<std::shared_ptr<IExpressionCommand>>>&) override {
        expression.clear();
        if (!tokens) return;
        unsigned int count=0; tokens->GetSize(&count);
        for (unsigned int i=0; i<count; i++) { std::pair<std::wstring,int> token; tokens->GetAt(i,&token); expression+=token.first; }
    }
    void SetParenthesisNumber(unsigned int) override {}
    void OnNoRightParenAdded() override {}
    void MaxDigitsReached() override {}
    void BinaryOperatorReceived() override {}
    void OnHistoryItemAdded(unsigned int) override {}
    void SetMemorizedNumbers(const std::vector<std::wstring>& values) override { memory=values; }
    void MemoryItemChanged(unsigned int) override {}
    const char* snapshot() {
        buffer = "{\"display\":"+quote(display)+",\"expression\":"+quote(expression)+",\"error\":"+(error?"true":"false")+",\"memory\":[";
        for (size_t i=0;i<memory.size();i++) { if(i) buffer+=","; buffer+=quote(memory[i]); }
        buffer+="],\"history\":[";
        if(mode!=2) {
            const auto& history=manager->GetHistoryItems();
            for(size_t i=0;i<history.size();i++) {
                if(i) buffer+=",";
                const auto& item=history[i]->historyItemVector;
                buffer+="{\"expression\":"+quote(item.expression)+",\"result\":"+quote(item.result)+"}";
            }
        }
        buffer+="]}"; return buffer.c_str();
    }
};
extern "C" {
void* calculator_create() { try { return new Engine(); } catch(...) { return nullptr; } }
void* calculator_create_localized(const char* const* keys, const char* const* values, int count) {
    try { if(count<0 || count>1024) return nullptr; return new Engine(keys,values,count); } catch(...) { return nullptr; }
}
void calculator_destroy(Engine* e) { delete e; }
const char* calculator_snapshot(Engine* e) { try { return e->snapshot(); } catch(...) { return nullptr; } }
int calculator_command(Engine* e, int command) {
    try { e->activate(); e->manager->SendCommand(static_cast<Command>(command)); return 0; } catch(...) { return -1; }
}
int calculator_mode(Engine* e, int mode) {
    try {
        e->activate();
        if (mode<0 || mode>2) return -1;
        if(mode==0) e->manager->SetStandardMode();
        else if(mode==1) e->manager->SetScientificMode();
        else e->manager->SetProgrammerMode();
        e->mode=mode; return 0;
    } catch(...) { return -1; }
}
int calculator_memory(Engine* e, int action, unsigned int index) {
    try {
        e->activate();
        if(action!=0 && action!=5 && index>=e->memory.size()) return -1;
        switch(action) {
            case 0: e->manager->MemorizeNumber(); break;
            case 1: e->manager->MemorizedNumberLoad(index); break;
            case 2: e->manager->MemorizedNumberAdd(index); break;
            case 3: e->manager->MemorizedNumberSubtract(index); break;
            case 4: e->manager->MemorizedNumberClear(index); break;
            case 5: e->manager->MemorizedNumberClearAll(); break;
            default: return -1;
        }
        return 0;
    } catch(...) { return -1; }
}
const char* calculator_radix(Engine* e, int radix) {
    try { e->activate(); e->buffer=utf8(e->manager->GetResultForRadix(radix,64)); return e->buffer.c_str(); } catch(...) { return nullptr; }
}
int calculator_clear_history(Engine* e) { try { if(e->mode!=2) e->manager->ClearHistory(); return 0; } catch(...) { return -1; } }
}
