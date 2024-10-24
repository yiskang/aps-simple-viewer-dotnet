/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Developer Advocacy and Support
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

$(document).ready(function () {
  prepareAppBucketTree();
  $('#refreshBuckets').click(function () {
    $('#appBuckets').jstree(true).refresh();
  });

  $('#createNewBucket').click(function () {
    createNewBucket();
  });

  $('#createBucketModal').on('shown.bs.modal', function () {
    $("#newBucketKey").focus();
  });

  $('#submitTranslation').click(function () {
    var treeNode = $('#appBuckets').jstree(true).get_selected(true)[0];
    submitTranslation(treeNode);
  });

  $('#CompositeTranslationModal').on('shown.bs.modal', function () {
    $("#rootDesignFilename").focus();
  });

  $('#hiddenUploadField').change(function () {
    let node = $('#appBuckets').jstree(true).get_selected(true)[0];
    let _this = this;
    if (_this.files.length == 0) return;
    let file = _this.files[0];
    switch (node.type) {
      case 'bucket':
        let formData = new FormData();
        formData.append('model-file', file);
        formData.append('bucket-key', node.id);

        $.ajax({
          url: '/api/models',
          data: formData,
          processData: false,
          contentType: false,
          type: 'POST',
          success: function (data) {
            $('#appBuckets').jstree(true).refresh_node(node);
            _this.value = '';
          }
        });
        break;
    }
  });
});

function createNewBucket() {
  let bucketKey = $('#newBucketKey').val();
  let policyKey = $('#newBucketPolicyKey').val();

  const regex = /^[\-_.a-z0-9]{3,128}$/g;
  if (!bucketKey.match(regex)) {
    alert('The bucket name can only contain characters -_.a-z0-9 and it must be between 3 and 128 characters long.');
    return;
  }

  jQuery.post({
    url: '/api/buckets',
    contentType: 'application/json',
    data: JSON.stringify({ 'bucketKey': bucketKey, 'policyKey': policyKey }),
    success: function (res) {
      $('#appBuckets').jstree(true).refresh();
      $('#createBucketModal').modal('toggle');
    },
    error: function (err) {
      if (err.status == 409)
        alert('Bucket already exists - 409: Duplicated')
      console.log(err);
    }
  });
}

function prepareAppBucketTree() {
  $('#appBuckets').jstree({
    'core': {
      'themes': { "icons": true },
      'data': {
        "url": '/api/buckets',
        "dataType": "json",
        'multiple': false,
        "data": function (node) {
          return { "id": node.id };
        }
      }
    },
    'types': {
      'default': {
        'icon': 'glyphicon glyphicon-question-sign'
      },
      '#': {
        'icon': 'glyphicon glyphicon-cloud'
      },
      'bucket': {
        'icon': 'glyphicon glyphicon-folder-open'
      },
      'object': {
        'icon': 'glyphicon glyphicon-file'
      }
    },
    "plugins": ["types", "state", "sort", "contextmenu"],
    contextmenu: { items: autodeskCustomMenu }
    // }).on('loaded.jstree', function () {
    //   $('#appBuckets').jstree('open_all');
  }).bind("activate_node.jstree", function (evt, data) {
    if (data != null && data.node != null && data.node.type == 'object') {
      $("#apsViewer").empty();
      let urn = data.node.id;
      getApsToken(function (access_token) {
        jQuery.ajax({
          url: `/api/models/${urn}/status`,
          success: function (res) {
            if (res.status === 'success' || res.status === 'complete') launchViewer(urn);
            else $("#apsViewer").html('The translation job still running: ' + res.progress + '. Please try again in a moment.');
          },
          error: function (err) {
            let msgButton = 'This file is not translated yet! ' +
              '<button class="btn btn-xs btn-info" onclick="translateObject()"><span class="glyphicon glyphicon-eye-open"></span> ' +
              'Start translation</button>'
            $("#apsViewer").html(msgButton);
          }
        });
      })
    }
  });
}

function autodeskCustomMenu(autodeskNode) {
  let items = null;

  switch (autodeskNode.type) {
    case "bucket":
      items = {
        uploadFile: {
          label: "Upload file",
          action: function () {
            uploadFile();
          },
          icon: 'glyphicon glyphicon-cloud-upload'
        }
      };
      break;
    case "object":
      items = {
        translateFile: {
          label: "Translate",
          action: function () {
            let treeNode = $('#appBuckets').jstree(true).get_selected(true)[0];
            translateObject(treeNode);
          },
          icon: 'glyphicon glyphicon-eye-open'
        }
      };
      break;
  }

  return items;
}

function uploadFile() {
  $('#hiddenUploadField').click();
}

function translateObject(node) {
  $("#apsViewer").empty();
  if (node == null) node = $('#appBuckets').jstree(true).get_selected(true)[0];
  let bucketKey = node.parents[0];
  let urn = node.id;
  let objectKey = node.name;

  jQuery.post({
    url: `/api/models/${urn}/jobs`,
    contentType: 'application/json',
    data: JSON.stringify({ 'bucketKey': bucketKey, 'objectName': objectKey }),
    success: function (res) {
      $("#apsViewer").html('Translation started! Please try again in a moment.');
    },
  });
}

function submitTranslation(node) {
  $("#apsViewer").empty();
  if (node == null) node = $('#appBuckets').jstree(true).get_selected(true)[0];
  let bucketKey = node.parents[0];
  let urn = node.id;
  let objectKey = node.text;

  let rootDesignFilename = $('#rootDesignFilename').val();
  // let isSvf2 = $('#outputFormat :selected').text() === 'SVF2';
  let xAdsForce = ($('#xAdsForce').is(':checked') === true);
  let data = { 'bucketKey': bucketKey, 'objectName': objectKey, /*'isSvf2': (isSvf2 === true),*/ 'xAdsForce': xAdsForce };

  if ((rootDesignFilename && rootDesignFilename.trim() && rootDesignFilename.trim().length > 0)) {
    data.rootFilename = rootDesignFilename;
  }

  jQuery.post({
    url: `/api/models/${urn}/jobs`,
    contentType: 'application/json',
    data:  JSON.stringify(data),
    success: function (res) {
      $('#CompositeTranslationModal').modal('hide');
      $("#apsViewer").html('Translation started! Please try again in a moment.');
    },
  });
}

function translateObject() {
  $('#CompositeTranslationModal').modal('show');
}