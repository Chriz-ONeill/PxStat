/*******************************************************************************
Application - Library 
*******************************************************************************/
var app = app || {};
app.library = app.library || {};

app.library.user = {};
app.library.user.data = {};
app.library.user.data.CcnUsername = null;
app.library.user.data.CcnName = null;
app.library.user.modal = {};
app.library.user.modal.ajax = {};
app.library.user.modal.callback = {};

//#region User
/**
 * Get User data to read user details
 * @param {*} apiParam
 */
app.library.user.modal.read = function (apiParams) {
  // default params
  apiParams = apiParams || {};
  // Get data from API
  api.ajax.jsonrpc.request(
    app.config.url.api.private,
    "PxStat.Security.Account_API.Read",
    apiParams,
    "app.library.user.modal.callback.read"
  );
};

/**
 * Get User data to read current user details
 */
app.library.user.modal.readCurrent = function () {
  // Get data from API
  api.ajax.jsonrpc.request(
    app.config.url.api.private,
    "PxStat.Security.Account_API.ReadCurrent",
    null,
    "app.library.user.modal.callback.read"
  );
};

/**
 * Populate User data to read user details
 * @param {*} data
 */
app.library.user.modal.callback.read = function (data) {
  if (data && (Array.isArray(data) && data.length)) {
    data = data[0];
    $("#modal-read-user").find("[name=ccn-username]").text(data.CcnUsername);
    $("#modal-read-user").find("[name=ccn-name]").text(data.CcnName);
    $("#modal-read-user").find("[name=prv-value]").text(app.label.datamodel.privilege[data.PrvValue]);
    $("#modal-read-user").find("[name=ccn-email]").html(app.library.html.email(data.CcnEmail));

    if (data.PrvCode != C_APP_PRIVILEGE_MODERATOR) {
      //user is admin or power user, no groups, hide row using d-none
      $("#modal-read-user").find("[name=container-list]").hide();
    } else {
      app.library.user.modal.buildGroupList(data);
      $("#modal-read-user").find("[name=container-list]").show();
    }

    // Unbind change event
    $("#modal-read-user").find("[name=notification]").off("change");

    // If the user if the current one, then allow toggle notifications
    if (data.CcnUsername == app.library.user.data.CcnUsername) {
      //initiate toggle buttons
      $("#modal-read-user").find("[name=notification]").bootstrapToggle("destroy").bootstrapToggle({
        on: app.label.static["true"],
        off: app.label.static["false"],
        onstyle: "success",
        offstyle: "warning",
        width: C_APP_TOGGLE_LENGTH //Depend on language translation.
      });

      //Set state of bootstrapToggle button
      $("#modal-read-user").find("[name=notification]").bootstrapToggle(data.CcnNotificationFlag ? "on" : "off");
      // Enable the notification
      $("#modal-read-user").find("[name=notification]").bootstrapToggle("enable");
      // Bind change event on toggle notification
      $("#modal-read-user").find("[name=notification]").once("change", function () {
        app.library.user.modal.ajax.update();
      });
    } else {
      //initiate toggle buttons
      $("#modal-read-user").find("[name=notification]").bootstrapToggle("destroy").bootstrapToggle({
        on: app.label.static["true"],
        off: app.label.static["false"],
        onstyle: "light",
        offstyle: "neutral",
        width: C_APP_TOGGLE_LENGTH //Depend on language translation.
      });

      //Set state of bootstrapToggle button
      $("#modal-read-user").find("[name=notification]").bootstrapToggle(data.CcnNotificationFlag ? "on" : "off");
      // Disable the notification
      $("#modal-read-user").find("[name=notification]").bootstrapToggle("disable");
    }

    // Switch between the modals to avoid overlapping
    $("#modal-read-group").modal("hide");
    $("#modal-read-user").modal("show");
  }
  // Handle no data
  else
    api.modal.information(app.label.static["api-ajax-nodata"]);
};

/**
 * Build Group List for the User
 * @param {*} data
 */
app.library.user.modal.buildGroupList = function (data) {
  //Flush the list
  $("#modal-read-user .list-group").empty(); //Do not delete.
  // Generate links for list of the Groups for the User
  $.each(data.UserGroups, function (key, value) {
    var userIconClass;
    if (value.GccApproveFlag) {
      //set class for icon depending on approver or not
      userIconClass = "fas fa-user-check text-success";
      //set title for Bootstrap tooltip
      userTooltipTitle = app.label.static["approver"];
    } else {
      userIconClass = "fas fa-user-edit text-orange";
      //set title for Bootstrap tooltip
      userTooltipTitle = app.label.static["editor"];
    }
    //Create Group Link.
    var linkGroup = $("<a>", {
      idn: value.GrpCode,
      href: "#",
      html: $("<i>", {
        "data-toggle": "tooltip",
        "data-placement": "top",
        "title": "", //userTooltipTitle,
        "data-original-title": userTooltipTitle,
        class: userIconClass
      }).get(0).outerHTML + " " + value.GrpName
    }).get(0);
    linkGroup.addEventListener("click", function (e) {
      e.preventDefault();
      app.library.group.modal.read(value.GrpCode);
    });
    var li = $("<li>", {
      class: "list-group-item"
    }).html(linkGroup);
    $("#modal-read-user .list-group").append(li);
    //Bootstrap tooltip
    $('[data-toggle="tooltip"]').tooltip();
  });
};

//#endregion

//#region Update
/**
 * Ajax call Update User
 * Save updated User via AJAX call.
 */
app.library.user.modal.ajax.update = function () {
  api.ajax.jsonrpc.request(
    app.config.url.api.private,
    "PxStat.Security.Account_API.UpdateCurrent",
    { CcnNotificationFlag: $("#modal-read-user").find("[name=notification]").prop('checked') },
    "app.library.user.modal.callback.update",
    null,
    "app.library.user.modal.callback.updateOnError",
    null,
    { async: false }
  );
};

/**
 * Update User Callback
 * After AJAX call.
 * @param {*} data 
 */
app.library.user.modal.callback.update = function (data) {
  if (data == C_APP_API_SUCCESS) {
    api.modal.success(app.library.html.parseDynamicLabel("success-record-updated", [""]));
  }
  // Handle exception
  else api.modal.exception(app.label.static["api-ajax-exception"]);
};

/**
 * Update User Callback on Error
 * After AJAX call.
 * @param {*} error 
 */
app.library.user.modal.callback.updateOnError = function (error) {
  // Hide modal
  $("#modal-read-user").modal("hide");
  // Force reload
  app.library.user.modal.readCurrent();
};
//#endregion

