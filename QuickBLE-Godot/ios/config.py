def can_build(env, plat):
    return plat=="iphone"

def configure(env):
    if env['platform'] == "iphone":
        env.Append(FRAMEWORKPATH=['#modules/quickble/lib'])
        env.Append(CPPPATH=['#core'])
        env.Append(LINKFLAGS=['-ObjC', '-framework', 'Foundation', '-framework', 'CoreBluetooth', '-framework', 'QuickBLE'])
