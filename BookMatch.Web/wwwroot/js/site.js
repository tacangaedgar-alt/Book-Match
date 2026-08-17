document.addEventListener('DOMContentLoaded',()=>{
 const sidebar=document.querySelector('#sidebar');document.querySelector('[data-sidebar-toggle]')?.addEventListener('click',()=>sidebar?.classList.toggle('open'));
 let modalTrigger=null;const closeModal=m=>{if(!m)return;m.hidden=true;document.body.style.overflow='';modalTrigger?.focus();};
 document.querySelectorAll('[data-modal-open]').forEach(b=>b.addEventListener('click',()=>{const m=document.getElementById(b.dataset.modalOpen);if(m){modalTrigger=b;m.hidden=false;document.body.style.overflow='hidden';m.querySelector('input,button,select')?.focus();}}));
 document.querySelectorAll('[data-modal-close]').forEach(b=>b.addEventListener('click',()=>closeModal(b.closest('.modal-shell'))));
 document.querySelectorAll('.modal-shell').forEach(m=>m.addEventListener('click',e=>{if(e.target===m)closeModal(m)}));
 document.addEventListener('keydown',e=>{if(e.key==='Escape')closeModal(document.querySelector('.modal-shell:not([hidden])'))});
 document.querySelectorAll('[data-submit-feedback]').forEach(form=>form.addEventListener('submit',()=>setSubmitting(form,'Publicando…')));

 const quiz=document.querySelector('[data-quiz]');
 if(quiz){
  const questions=[
   {field:'Genre',text:'¿Cuál es tu género literario favorito?',options:['Ficción','Ciencia Ficción','Romance','Thriller','No Ficción','Historia','Filosofía','Poesía','Aventura','Drama']},
   {field:'PagePreference',text:'¿Cuántas páginas prefieres en un libro?',options:['Menos de 150 páginas','150–300 páginas','300–500 páginas','Más de 500 páginas','No tengo preferencia']},
   {field:'Language',text:'¿En qué idioma prefieres leer?',options:['Español','Inglés','Portugués','Francés','Otro']},
   {field:'Format',text:'¿Qué formato prefieres?',options:['Leer en línea','Descargar PDF','Epub / eReader','Sin preferencia']},
   {field:'Pace',text:'¿Qué ritmo narrativo disfrutas?',options:['Ágil y dinámico','Equilibrado','Lento y reflexivo']},
   {field:'Mood',text:'¿Qué ambiente buscas?',options:['Inspirador','Misterioso','Romántico','Realista','Épico']},
   {field:'Discovery',text:'¿Cómo descubres nuevos libros?',options:['Recomendaciones','Autores favoritos','Tendencias','Explorando géneros']}
  ];
  let step=0;const answers={};const intro=quiz.querySelector('.quiz-intro'),card=quiz.querySelector('.quiz-card'),result=quiz.querySelector('.quiz-results');
  const render=()=>{const q=questions[step];quiz.querySelector('[data-step-label]').textContent=`Pregunta ${step+1} de ${questions.length}`;quiz.querySelector('[data-progress-label]').textContent=`${Math.round(step/questions.length*100)}% completado`;quiz.querySelector('[data-progress]').style.width=`${step/questions.length*100}%`;quiz.querySelector('[data-question]').textContent=q.text;const options=quiz.querySelector('[data-options]');options.innerHTML='';q.options.forEach(value=>{const button=document.createElement('button');button.type='button';button.textContent=value;if(answers[q.field]===value)button.classList.add('selected');button.addEventListener('click',()=>{answers[q.field]=value;quiz.querySelector(`[data-answer="${q.field}"]`).value=value;if(step===questions.length-1){card.hidden=true;result.hidden=false;result.querySelector('button')?.focus()}else{step++;render()}});options.appendChild(button)})};
  quiz.querySelector('[data-quiz-start]').addEventListener('click',()=>{intro.hidden=true;card.hidden=false;render();card.querySelector('button')?.focus()});
  quiz.querySelector('[data-back]').addEventListener('click',()=>{if(step===0){card.hidden=true;intro.hidden=false}else{step--;render()}});
  quiz.querySelector('[data-back-final]').addEventListener('click',()=>{result.hidden=true;card.hidden=false;step=questions.length-1;render()});
  quiz.querySelector('[data-recommendation-form]').addEventListener('submit',event=>{if(Object.keys(answers).length!==questions.length){event.preventDefault();return}setSubmitting(event.currentTarget,'Calculando…')});
 }

 const reports=document.querySelector('[data-reports]');
 if(reports){
  const tabs=[...reports.querySelectorAll('[data-report-tab]')],panels=[...reports.querySelectorAll('[data-report-panel]')];
  const activate=tab=>{tabs.forEach(x=>{const active=x===tab;x.classList.toggle('active',active);x.setAttribute('aria-selected',String(active));x.tabIndex=active?0:-1});panels.forEach(x=>x.hidden=x.id!==tab.dataset.reportTab);tab.focus()};
  tabs.forEach((tab,index)=>{tab.addEventListener('click',()=>activate(tab));tab.addEventListener('keydown',event=>{if(!['ArrowLeft','ArrowRight','Home','End'].includes(event.key))return;event.preventDefault();let next=event.key==='Home'?0:event.key==='End'?tabs.length-1:(index+(event.key==='ArrowRight'?1:-1)+tabs.length)%tabs.length;activate(tabs[next])})});
  const activePanel=()=>panels.find(x=>!x.hidden);
  reports.querySelector('[data-export-excel]')?.addEventListener('click',()=>{const panel=activePanel(),table=panel?.querySelector('table');if(!table)return;const csv='\uFEFF'+[...table.rows].map(row=>[...row.cells].map(cell=>'"'+cell.innerText.replaceAll('"','""')+'"').join(';')).join('\r\n');const url=URL.createObjectURL(new Blob([csv],{type:'text/csv;charset=utf-8'}));const link=document.createElement('a');link.href=url;link.download=`BookMatch-${panel.dataset.reportName}.csv`;link.click();setTimeout(()=>URL.revokeObjectURL(url),1000)});
  reports.querySelector('[data-export-pdf]')?.addEventListener('click',()=>{const panel=activePanel();if(!panel)return;const oldTitle=document.title;document.title=`BookMatch-${panel.dataset.reportName}`;const restore=()=>{document.title=oldTitle;window.removeEventListener('afterprint',restore)};window.addEventListener('afterprint',restore);window.print();setTimeout(restore,2000)});
 }
 setTimeout(()=>document.querySelectorAll('.toast-message').forEach(x=>x.remove()),4500);
});
function setSubmitting(form,label){const button=form.querySelector('[data-submit-button]');if(button){button.disabled=true;button.textContent=label;button.setAttribute('aria-busy','true')}}
