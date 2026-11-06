local class = require("lib.class")
local M = class("Page_Lobby")

function M:ctor(ui)
    self.ui = ui
end

function M:OnShow()
    self.ui:SetText("Label", "hello")
end

return M
