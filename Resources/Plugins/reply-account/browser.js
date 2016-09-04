enabled(){
  var configuration = {};
  
  window.TDPF_loadConfigurationFile(this, "user.configuration.js", obj => configuration = obj);
  
  this.uiInlineComposeTweetEvent = function(e, data){
    var account = null;
    
    if (configuration.useAdvancedSelector && configuration.customSelector){
      var column = TD.controller.columnManager.get(data.element.closest("section.column").attr("data-column"));
      var result = configuration.customSelector(column);
      
      if (typeof result === "string" && result[0] === '@'){
        account = result.substring(1);
      }
    }
    
    if (account === null){
      if (configuration.defaultAccount === false){
        return;
      }
      else if (configuration.defaultAccount !== "" && configuration.defaultAccount[0] === '@'){
        account = configuration.defaultAccount.substring(1);
      }
    }
    
    var identifier;
    
    if (account === null){
      identifier = TD.storage.clientController.client.getDefaultAccount();
    }
    else{
      var obj = TD.storage.accountController.getAccountFromUsername(account);
      
      if (obj.length === 0){
        return;
      }
      else{
        identifier = obj[0].privateState.key;
      }
    }
    
    data.singleFrom = data.from = [ identifier ];
  };
}

ready(){
  $(document).on("uiInlineComposeTweet", this.uiInlineComposeTweetEvent);
}

disabled(){
  $(document).off("uiInlineComposeTweet", this.uiInlineComposeTweetEvent);
}