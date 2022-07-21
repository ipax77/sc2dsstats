/*!
 * chartjs-plugin-datalabels v1.0.0
 * https://chartjs-plugin-datalabels.netlify.app
 * (c) 2017-2021 chartjs-plugin-datalabels contributors
 * Released under the MIT license
 */
!function(t,e){"object"==typeof exports&&"undefined"!=typeof module?module.exports=e(require("chart.js")):"function"==typeof define&&define.amd?define(["chart.js"],e):(t="undefined"!=typeof globalThis?globalThis:t||self).ChartDataLabels=e(t.Chart)}(this,(function(t){"use strict";function e(t){return t&&"object"==typeof t&&"default"in t?t:{default:t}}var r=e(t),n=r.default.helpers,a=function(){if("undefined"!=typeof window){if(window.devicePixelRatio)return window.devicePixelRatio;var t=window.screen;if(t)return(t.deviceXDPI||1)/(t.logicalXDPI||1)}return 1}(),i={toTextLines:function(t){var e,r=[];for(t=[].concat(t);t.length;)"string"==typeof(e=t.pop())?r.unshift.apply(r,e.split("\n")):Array.isArray(e)?t.push.apply(t,e):n.isNullOrUndef(t)||r.unshift(""+e);return r},toFontString:function(t){return!t||n.isNullOrUndef(t.size)||n.isNullOrUndef(t.family)?null:(t.style?t.style+" ":"")+(t.weight?t.weight+" ":"")+t.size+"px "+t.family},textSize:function(t,e,r){var n,a=[].concat(e),i=a.length,o=t.font,l=0;for(t.font=r.string,n=0;n<i;++n)l=Math.max(t.measureText(a[n]).width,l);return t.font=o,{height:i*r.lineHeight,width:l}},parseFont:function(t){var e=r.default.defaults.global,a=n.valueOrDefault(t.size,e.defaultFontSize),o={family:n.valueOrDefault(t.family,e.defaultFontFamily),lineHeight:n.options.toLineHeight(t.lineHeight,a),size:a,style:n.valueOrDefault(t.style,e.defaultFontStyle),weight:n.valueOrDefault(t.weight,null),string:""};return o.string=i.toFontString(o),o},bound:function(t,e,r){return Math.max(t,Math.min(e,r))},arrayDiff:function(t,e){var r,n,a,i,o=t.slice(),l=[];for(r=0,a=e.length;r<a;++r)i=e[r],-1===(n=o.indexOf(i))?l.push([i,1]):o.splice(n,1);for(r=0,a=o.length;r<a;++r)l.push([o[r],-1]);return l},rasterize:function(t){return Math.round(t*a)/a}};function o(t,e){var r=e.x,n=e.y;if(null===r)return{x:0,y:-1};if(null===n)return{x:1,y:0};var a=t.x-r,i=t.y-n,o=Math.sqrt(a*a+i*i);return{x:o?a/o:0,y:o?i/o:-1}}function l(t,e,r){var n=0;return t<r.left?n|=1:t>r.right&&(n|=2),e<r.top?n|=8:e>r.bottom&&(n|=4),n}function s(t,e){var r,n,a=e.anchor,i=t;return e.clamp&&(i=function(t,e){for(var r,n,a,i=t.x0,o=t.y0,s=t.x1,u=t.y1,d=l(i,o,e),f=l(s,u,e);d|f&&!(d&f);)8&(r=d||f)?(n=i+(s-i)*(e.top-o)/(u-o),a=e.top):4&r?(n=i+(s-i)*(e.bottom-o)/(u-o),a=e.bottom):2&r?(a=o+(u-o)*(e.right-i)/(s-i),n=e.right):1&r&&(a=o+(u-o)*(e.left-i)/(s-i),n=e.left),r===d?d=l(i=n,o=a,e):f=l(s=n,u=a,e);return{x0:i,x1:s,y0:o,y1:u}}(i,e.area)),"start"===a?(r=i.x0,n=i.y0):"end"===a?(r=i.x1,n=i.y1):(r=(i.x0+i.x1)/2,n=(i.y0+i.y1)/2),function(t,e,r,n,a){switch(a){case"center":r=n=0;break;case"bottom":r=0,n=1;break;case"right":r=1,n=0;break;case"left":r=-1,n=0;break;case"top":r=0,n=-1;break;case"start":r=-r,n=-n;break;case"end":break;default:a*=Math.PI/180,r=Math.cos(a),n=Math.sin(a)}return{x:t,y:e,vx:r,vy:n}}(r,n,t.vx,t.vy,e.align)}var u=function(t,e){var r=(t.startAngle+t.endAngle)/2,n=Math.cos(r),a=Math.sin(r),i=t.innerRadius,o=t.outerRadius;return s({x0:t.x+n*i,y0:t.y+a*i,x1:t.x+n*o,y1:t.y+a*o,vx:n,vy:a},e)},d=function(t,e){var r=o(t,e.origin),n=r.x*t.radius,a=r.y*t.radius;return s({x0:t.x-n,y0:t.y-a,x1:t.x+n,y1:t.y+a,vx:r.x,vy:r.y},e)},f=function(t,e){var r=o(t,e.origin),n=t.x,a=t.y,i=0,l=0;return t.horizontal?(n=Math.min(t.x,t.base),i=Math.abs(t.base-t.x)):(a=Math.min(t.y,t.base),l=Math.abs(t.base-t.y)),s({x0:n,y0:a+l,x1:n+i,y1:a,vx:r.x,vy:r.y},e)},c=function(t,e){var r=o(t,e.origin);return s({x0:t.x,y0:t.y,x1:t.x,y1:t.y,vx:r.x,vy:r.y},e)},h=r.default.helpers,x=i.rasterize;function y(t){var e=t._model.horizontal,r=t._scale||e&&t._xScale||t._yScale;if(!r)return null;if(void 0!==r.xCenter&&void 0!==r.yCenter)return{x:r.xCenter,y:r.yCenter};var n=r.getBasePixel();return e?{x:n,y:null}:{x:null,y:n}}function v(t,e,r){var n=t.shadowBlur,a=r.stroked,i=x(r.x),o=x(r.y),l=x(r.w);a&&t.strokeText(e,i,o,l),r.filled&&(n&&a&&(t.shadowBlur=0),t.fillText(e,i,o,l),n&&a&&(t.shadowBlur=n))}var b=function(t,e,r,n){var a=this;a._config=t,a._index=n,a._model=null,a._rects=null,a._ctx=e,a._el=r};h.extend(b.prototype,{_modelize:function(t,e,n,a){var o,l=this,s=l._index,x=h.options.resolve,v=i.parseFont(x([n.font,{}],a,s)),b=x([n.color,r.default.defaults.global.defaultFontColor],a,s);return{align:x([n.align,"center"],a,s),anchor:x([n.anchor,"center"],a,s),area:a.chart.chartArea,backgroundColor:x([n.backgroundColor,null],a,s),borderColor:x([n.borderColor,null],a,s),borderRadius:x([n.borderRadius,0],a,s),borderWidth:x([n.borderWidth,0],a,s),clamp:x([n.clamp,!1],a,s),clip:x([n.clip,!1],a,s),color:b,display:t,font:v,lines:e,offset:x([n.offset,0],a,s),opacity:x([n.opacity,1],a,s),origin:y(l._el),padding:h.options.toPadding(x([n.padding,0],a,s)),positioner:(o=l._el,o instanceof r.default.elements.Arc?u:o instanceof r.default.elements.Point?d:o instanceof r.default.elements.Rectangle?f:c),rotation:x([n.rotation,0],a,s)*(Math.PI/180),size:i.textSize(l._ctx,e,v),textAlign:x([n.textAlign,"start"],a,s),textShadowBlur:x([n.textShadowBlur,0],a,s),textShadowColor:x([n.textShadowColor,b],a,s),textStrokeColor:x([n.textStrokeColor,b],a,s),textStrokeWidth:x([n.textStrokeWidth,0],a,s)}},update:function(t){var e,r,n,a=this,o=null,l=null,s=a._index,u=a._config,d=h.options.resolve([u.display,!0],t,s);d&&(e=t.dataset.data[s],r=h.valueOrDefault(h.callback(u.formatter,[e,t]),e),(n=h.isNullOrUndef(r)?[]:i.toTextLines(r)).length&&(l=function(t){var e=t.borderWidth||0,r=t.padding,n=t.size.height,a=t.size.width,i=-a/2,o=-n/2;return{frame:{x:i-r.left-e,y:o-r.top-e,w:a+r.width+2*e,h:n+r.height+2*e},text:{x:i,y:o,w:a,h:n}}}(o=a._modelize(d,n,u,t)))),a._model=o,a._rects=l},geometry:function(){return this._rects?this._rects.frame:{}},rotation:function(){return this._model?this._model.rotation:0},visible:function(){return this._model&&this._model.opacity},model:function(){return this._model},draw:function(t,e){var r,n=t.ctx,a=this._model,o=this._rects;this.visible()&&(n.save(),a.clip&&(r=a.area,n.beginPath(),n.rect(r.left,r.top,r.right-r.left,r.bottom-r.top),n.clip()),n.globalAlpha=i.bound(0,a.opacity,1),n.translate(x(e.x),x(e.y)),n.rotate(a.rotation),function(t,e,r){var n=r.backgroundColor,a=r.borderColor,i=r.borderWidth;(n||a&&i)&&(t.beginPath(),h.canvas.roundedRect(t,x(e.x)+i/2,x(e.y)+i/2,x(e.w)-i,x(e.h)-i,r.borderRadius),t.closePath(),n&&(t.fillStyle=n,t.fill()),a&&i&&(t.strokeStyle=a,t.lineWidth=i,t.lineJoin="miter",t.stroke()))}(n,o.frame,a),function(t,e,r,n){var a,i=n.textAlign,o=n.color,l=!!o,s=n.font,u=e.length,d=n.textStrokeColor,f=n.textStrokeWidth,c=d&&f;if(u&&(l||c))for(r=function(t,e,r){var n=r.lineHeight,a=t.w,i=t.x;return"center"===e?i+=a/2:"end"!==e&&"right"!==e||(i+=a),{h:n,w:a,x:i,y:t.y+n/2}}(r,i,s),t.font=s.string,t.textAlign=i,t.textBaseline="middle",t.shadowBlur=n.textShadowBlur,t.shadowColor=n.textShadowColor,l&&(t.fillStyle=o),c&&(t.lineJoin="round",t.lineWidth=f,t.strokeStyle=d),a=0,u=e.length;a<u;++a)v(t,e[a],{stroked:c,filled:l,w:r.w,x:r.x,y:r.y+r.h*a})}(n,a.lines,o.text,a),n.restore())}});var _=r.default.helpers,p=Number.MIN_SAFE_INTEGER||-9007199254740991,g=Number.MAX_SAFE_INTEGER||9007199254740991;function m(t,e,r){var n=Math.cos(r),a=Math.sin(r),i=e.x,o=e.y;return{x:i+n*(t.x-i)-a*(t.y-o),y:o+a*(t.x-i)+n*(t.y-o)}}function w(t,e){var r,n,a,i,o,l=g,s=p,u=e.origin;for(r=0;r<t.length;++r)a=(n=t[r]).x-u.x,i=n.y-u.y,o=e.vx*a+e.vy*i,l=Math.min(l,o),s=Math.max(s,o);return{min:l,max:s}}function k(t,e){var r=e.x-t.x,n=e.y-t.y,a=Math.sqrt(r*r+n*n);return{vx:(e.x-t.x)/a,vy:(e.y-t.y)/a,origin:t,ln:a}}var M=function(){this._rotation=0,this._rect={x:0,y:0,w:0,h:0}};function S(t,e,r){var n=e.positioner(t,e),a=n.vx,i=n.vy;if(!a&&!i)return{x:n.x,y:n.y};var o=r.w,l=r.h,s=e.rotation,u=Math.abs(o/2*Math.cos(s))+Math.abs(l/2*Math.sin(s)),d=Math.abs(o/2*Math.sin(s))+Math.abs(l/2*Math.cos(s)),f=1/Math.max(Math.abs(a),Math.abs(i));return u*=a*f,d*=i*f,u+=e.offset*a,d+=e.offset*i,{x:n.x+u,y:n.y+d}}_.extend(M.prototype,{center:function(){var t=this._rect;return{x:t.x+t.w/2,y:t.y+t.h/2}},update:function(t,e,r){this._rotation=r,this._rect={x:e.x+t.x,y:e.y+t.y,w:e.w,h:e.h}},contains:function(t){var e=this,r=e._rect;return!((t=m(t,e.center(),-e._rotation)).x<r.x-1||t.y<r.y-1||t.x>r.x+r.w+2||t.y>r.y+r.h+2)},intersects:function(t){var e,r,n,a=this._points(),i=t._points(),o=[k(a[0],a[1]),k(a[0],a[3])];for(this._rotation!==t._rotation&&o.push(k(i[0],i[1]),k(i[0],i[3])),e=0;e<o.length;++e)if(r=w(a,o[e]),n=w(i,o[e]),r.max<n.min||n.max<r.min)return!1;return!0},_points:function(){var t=this,e=t._rect,r=t._rotation,n=t.center();return[m({x:e.x,y:e.y},n,r),m({x:e.x+e.w,y:e.y},n,r),m({x:e.x+e.w,y:e.y+e.h},n,r),m({x:e.x,y:e.y+e.h},n,r)]}});var $={prepare:function(t){var e,r,n,a,i,o=[];for(e=0,n=t.length;e<n;++e)for(r=0,a=t[e].length;r<a;++r)i=t[e][r],o.push(i),i.$layout={_box:new M,_hidable:!1,_visible:!0,_set:e,_idx:r};return o.sort((function(t,e){var r=t.$layout,n=e.$layout;return r._idx===n._idx?n._set-r._set:n._idx-r._idx})),this.update(o),o},update:function(t){var e,r,n,a,i,o=!1;for(e=0,r=t.length;e<r;++e)a=(n=t[e]).model(),(i=n.$layout)._hidable=a&&"auto"===a.display,i._visible=n.visible(),o|=i._hidable;o&&function(t){var e,r,n,a,i,o;for(e=0,r=t.length;e<r;++e)(a=(n=t[e]).$layout)._visible&&(i=n.geometry(),o=S(n._el._model,n.model(),i),a._box.update(o,i,n.rotation()));(function(t,e){var r,n,a,i;for(r=t.length-1;r>=0;--r)for(a=t[r].$layout,n=r-1;n>=0&&a._visible;--n)(i=t[n].$layout)._visible&&a._box.intersects(i._box)&&e(a,i)})(t,(function(t,e){var r=t._hidable,n=e._hidable;r&&n||n?e._visible=!1:r&&(t._visible=!1)}))}(t)},lookup:function(t,e){var r,n;for(r=t.length-1;r>=0;--r)if((n=t[r].$layout)&&n._visible&&n._box.contains(e))return t[r];return null},draw:function(t,e){var r,n,a,i,o,l;for(r=0,n=e.length;r<n;++r)(i=(a=e[r]).$layout)._visible&&(o=a.geometry(),l=S(a._el._view,a.model(),o),i._box.update(l,o,a.rotation()),a.draw(t,l))}},C=r.default.helpers,z={align:"center",anchor:"center",backgroundColor:null,borderColor:null,borderRadius:0,borderWidth:0,clamp:!1,clip:!1,color:void 0,display:!0,font:{family:void 0,lineHeight:1.2,size:void 0,style:void 0,weight:null},formatter:function(t){if(C.isNullOrUndef(t))return null;var e,r,n,a=t;if(C.isObject(t))if(C.isNullOrUndef(t.label))if(C.isNullOrUndef(t.r))for(a="",n=0,r=(e=Object.keys(t)).length;n<r;++n)a+=(0!==n?", ":"")+e[n]+": "+t[e[n]];else a=t.r;else a=t.label;return""+a},labels:void 0,listeners:{},offset:4,opacity:1,padding:{top:4,right:4,bottom:4,left:4},rotation:0,textAlign:"start",textStrokeColor:void 0,textStrokeWidth:0,textShadowBlur:0,textShadowColor:void 0},A=r.default.helpers,O="$default";function D(t,e,r){if(e){var n,a=r.$context,i=r.$groups;e[i._set]&&(n=e[i._set][i._key])&&!0===A.callback(n,[a])&&(t.$datalabels._dirty=!0,r.update(a))}}function N(t,e){var r,n,a=t.$datalabels,i=a._listeners;if(i.enter||i.leave){if("mousemove"===e.type)n=$.lookup(a._labels,e);else if("mouseout"!==e.type)return;r=a._hovered,a._hovered=n,function(t,e,r,n){var a,i;(r||n)&&(r?n?r!==n&&(i=a=!0):i=!0:a=!0,i&&D(t,e.leave,r),a&&D(t,e.enter,n))}(t,i,r,n)}}return r.default.defaults.global.plugins.datalabels=z,{id:"datalabels",beforeInit:function(t){t.$datalabels={_actives:[]}},beforeUpdate:function(t){var e=t.$datalabels;e._listened=!1,e._listeners={},e._datasets=[],e._labels=[]},afterDatasetUpdate:function(t,e,r){var n,a,i,o,l,s,u,d,f=e.index,c=t.$datalabels,h=c._datasets[f]=[],x=t.isDatasetVisible(f),y=t.data.datasets[f],v=function(t,e){var r,n,a,i=t.datalabels,o=[];return!1===i?null:(!0===i&&(i={}),e=A.merge({},[e,i]),n=e.labels||{},a=Object.keys(n),delete e.labels,a.length?a.forEach((function(t){n[t]&&o.push(A.merge({},[e,n[t],{_key:t}]))})):o.push(e),r=o.reduce((function(t,e){return A.each(e.listeners||{},(function(r,n){t[n]=t[n]||{},t[n][e._key||O]=r})),delete e.listeners,t}),{}),{labels:o,listeners:r})}(y,r),_=e.meta.data||[],p=t.ctx;for(p.save(),n=0,i=_.length;n<i;++n)if((u=_[n]).$datalabels=[],x&&u&&!u.hidden&&!u._model.skip)for(a=0,o=v.labels.length;a<o;++a)s=(l=v.labels[a])._key,(d=new b(l,p,u,n)).$groups={_set:f,_key:s||O},d.$context={active:!1,chart:t,dataIndex:n,dataset:y,datasetIndex:f},d.update(d.$context),u.$datalabels.push(d),h.push(d);p.restore(),A.merge(c._listeners,v.listeners,{merger:function(t,r,n){r[t]=r[t]||{},r[t][e.index]=n[t],c._listened=!0}})},afterUpdate:function(t,e){t.$datalabels._labels=$.prepare(t.$datalabels._datasets,e)},afterDatasetsDraw:function(t){$.draw(t,t.$datalabels._labels)},beforeEvent:function(t,e){if(t.$datalabels._listened)switch(e.type){case"mousemove":case"mouseout":N(t,e);break;case"click":!function(t,e){var r=t.$datalabels,n=r._listeners.click,a=n&&$.lookup(r._labels,e);a&&D(t,n,a)}(t,e)}},afterEvent:function(t){var e,n,a,o,l,s,u,d=t.$datalabels,f=d._actives,c=d._actives=t.lastActive||[],h=i.arrayDiff(f,c);for(e=0,n=h.length;e<n;++e)if((l=h[e])[1])for(a=0,o=(u=l[0].$datalabels||[]).length;a<o;++a)(s=u[a]).$context.active=1===l[1],s.update(s.$context);(d._dirty||h.length)&&($.update(d._labels),function(t){if(!t.animating){for(var e=r.default.animationService.animations,n=0,a=e.length;n<a;++n)if(e[n].chart===t)return;t.render({duration:1,lazy:!0})}}(t)),delete d._dirty}}}));
