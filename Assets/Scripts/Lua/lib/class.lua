-- 最小 class：M.new(M, ...) 建实例并调用 ctor（与 LuaScreen 的调用约定一致）
local function class(name)
    local cls = { __name = name }
    cls.__index = cls
    function cls.new(self, ...)
        local instance = setmetatable({}, self)
        if instance.ctor then
            instance:ctor(...)
        end
        return instance
    end
    return cls
end

return class
