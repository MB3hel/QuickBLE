#include <core/class_db.h>
#include <core/engine.h>

#include "register_types.h"
#include "src/QuickBLESingleton.h"

void register_quickble_types(){
    // Setup QuickBLESingleton as a singleton (allow interfacing with it the same way as with android)
    Engine::get_singleton()->add_singleton(Engine::Singleton("QuickBLESingleton", memnew(QuickBLESingleton)));
}

void unregister_quickble_types(){
    
}
