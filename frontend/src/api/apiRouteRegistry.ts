import { approvalsApi } from './approvals';
import { auditApi } from './audit';
import { authApi } from './auth';
import { delegationsApi } from './delegations';
import { documentMatrixApi, documentRequestsApi } from './documentRequests';
import { employeeBatchesApi } from './employeeBatches';
import { employeesApi } from './employees';
import { incidentsApi } from './incidents';
import { matrixApi } from './matrix';
import { photoReviewsApi } from './photoReviews';
import { projectsApi } from './projects';
import { reportsApi } from './reports';
import { siteVisitJournalApi } from './siteVisitJournal';
import { subcontractorsApi } from './subcontractors';
import { usersApi } from './users';
import { zupApi } from './zup';

export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

export type ApiRouteEntry =
  | {
      id: string;
      kind: 'http';
      method: HttpMethod;
      pathPattern: RegExp;
      invoke: () => Promise<unknown> | unknown;
    }
  | {
      id: string;
      kind: 'url';
      pathPattern: RegExp;
      invoke: () => string;
    };
    
export const EXPECTED_API_ROUTE_COUNT = 102;

const sampleId = '00000000-0000-4000-8000-000000000001';
const sampleProjectOid = '00000000-0000-4000-8000-000000000002';
const sampleFile = new File(['x'], 'test.jpg', { type: 'image/jpeg' });

export const apiRouteRegistry: ApiRouteEntry[] = [
  // auth
  { id: 'auth.login', kind: 'http', method: 'POST', pathPattern: /^\/api\/auth\/login$/, invoke: () => authApi.login('a@b.c', 'pwd') },
  { id: 'auth.devLogin', kind: 'http', method: 'POST', pathPattern: /^\/api\/auth\/dev-login$/, invoke: () => authApi.devLogin('a@b.c') },
  { id: 'auth.changePassword', kind: 'http', method: 'POST', pathPattern: /^\/api\/auth\/change-password$/, invoke: () => authApi.changePassword('old', 'new') },
  { id: 'auth.me', kind: 'http', method: 'GET', pathPattern: /^\/api\/auth\/me$/, invoke: () => authApi.me() },
  { id: 'auth.myProjects', kind: 'http', method: 'GET', pathPattern: /^\/api\/auth\/me\/projects$/, invoke: () => authApi.myProjects() },
  {
    id: 'auth.reportProjectProgress',
    kind: 'http',
    method: 'PUT',
    pathPattern: new RegExp(`^/api/auth/me/projects/${sampleProjectOid}/progress$`),
    invoke: () => authApi.reportProjectProgress(sampleProjectOid, 50)
  },

  // users
  { id: 'users.list', kind: 'http', method: 'GET', pathPattern: /^\/api\/users$/, invoke: () => usersApi.list() },
  { id: 'users.create', kind: 'http', method: 'POST', pathPattern: /^\/api\/users$/, invoke: () => usersApi.create({ fullName: 'T', position: 'P', email: 'u@test.kz', userType: 'TANSU' }) },
  {
    id: 'users.update',
    kind: 'http',
    method: 'PUT',
    pathPattern: new RegExp(`^/api/users/${sampleId}$`),
    invoke: () => usersApi.update(sampleId, { fullName: 'T', position: 'P', isActive: true })
  },
  {
    id: 'users.blocks',
    kind: 'http',
    method: 'GET',
    pathPattern: new RegExp(`^/api/users/${sampleId}/blocks$`),
    invoke: () => usersApi.blocks(sampleId)
  },
  {
    id: 'users.resetPassword',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/users/${sampleId}/reset-password$`),
    invoke: () => usersApi.resetPassword(sampleId)
  },

  // subcontractors
  { id: 'subcontractors.list', kind: 'http', method: 'GET', pathPattern: /^\/api\/subcontractors$/, invoke: () => subcontractorsApi.list() },
  { id: 'subcontractors.create', kind: 'http', method: 'POST', pathPattern: /^\/api\/subcontractors$/, invoke: () => subcontractorsApi.create('T', '123') },
  {
    id: 'subcontractors.update',
    kind: 'http',
    method: 'PUT',
    pathPattern: new RegExp(`^/api/subcontractors/${sampleId}$`),
    invoke: () => subcontractorsApi.update(sampleId, { name: 'T', bin: '123' })
  },
  {
    id: 'subcontractors.remove',
    kind: 'http',
    method: 'DELETE',
    pathPattern: new RegExp(`^/api/subcontractors/${sampleId}$`),
    invoke: () => subcontractorsApi.remove(sampleId)
  },
  {
    id: 'subcontractors.projects',
    kind: 'http',
    method: 'GET',
    pathPattern: new RegExp(`^/api/subcontractors/${sampleId}/projects$`),
    invoke: () => subcontractorsApi.projects(sampleId)
  },
  {
    id: 'subcontractors.bindProject',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/subcontractors/${sampleId}/projects$`),
    invoke: () => subcontractorsApi.bindProject(sampleId, sampleProjectOid, 'construction')
  },
  {
    id: 'subcontractors.unbindProject',
    kind: 'http',
    method: 'DELETE',
    pathPattern: new RegExp(`^/api/subcontractors/${sampleId}/projects/${sampleProjectOid}$`),
    invoke: () => subcontractorsApi.unbindProject(sampleId, sampleProjectOid)
  },
  {
    id: 'subcontractors.documents',
    kind: 'http',
    method: 'GET',
    pathPattern: new RegExp(`^/api/subcontractors/${sampleId}/documents$`),
    invoke: () => subcontractorsApi.documents(sampleId)
  },
  {
    id: 'subcontractors.uploadDocument',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/subcontractors/${sampleId}/documents$`),
    invoke: () => subcontractorsApi.uploadDocument(sampleId, new FormData())
  },
  {
    id: 'subcontractors.documentUrl',
    kind: 'url',
    pathPattern: new RegExp(`^/api/subcontractors/${sampleId}/documents/${sampleProjectOid}$`),
    invoke: () => subcontractorsApi.documentUrl(sampleId, sampleProjectOid)
  },
  {
    id: 'subcontractors.deleteDocument',
    kind: 'http',
    method: 'DELETE',
    pathPattern: new RegExp(`^/api/subcontractors/${sampleId}/documents/${sampleProjectOid}$`),
    invoke: () => subcontractorsApi.deleteDocument(sampleId, sampleProjectOid)
  },

  // projects
  { id: 'projects.list', kind: 'http', method: 'GET', pathPattern: /^\/api\/projects$/, invoke: () => projectsApi.list() },
  {
    id: 'projects.bindOptions',
    kind: 'http',
    method: 'GET',
    pathPattern: /^\/api\/projects\/bind-options$/,
    invoke: () => projectsApi.bindOptions()
  },
  { id: 'projects.register', kind: 'http', method: 'POST', pathPattern: /^\/api\/projects$/, invoke: () => projectsApi.register(sampleProjectOid) },
  {
    id: 'projects.get',
    kind: 'http',
    method: 'GET',
    pathPattern: new RegExp(`^/api/projects/${sampleProjectOid}$`),
    invoke: () => projectsApi.get(sampleProjectOid)
  },
  {
    id: 'projects.update',
    kind: 'http',
    method: 'PUT',
    pathPattern: new RegExp(`^/api/projects/${sampleProjectOid}$`),
    invoke: () => projectsApi.update(sampleProjectOid, { name: 'P' })
  },
  { id: 'projects.staffOptions', kind: 'http', method: 'GET', pathPattern: /^\/api\/projects\/staff-options$/, invoke: () => projectsApi.staffOptions() },
  {
    id: 'projects.uploadDocument',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/projects/${sampleProjectOid}/documents$`),
    invoke: () => projectsApi.uploadDocument(sampleProjectOid, sampleFile, 'doc', 'other')
  },
  {
    id: 'projects.documentUrl',
    kind: 'url',
    pathPattern: new RegExp(`^http://test/api/projects/${sampleProjectOid}/documents/${sampleId}$`),
    invoke: () => projectsApi.documentUrl(sampleProjectOid, sampleId)
  },
  {
    id: 'projects.deleteDocument',
    kind: 'http',
    method: 'DELETE',
    pathPattern: new RegExp(`^/api/projects/${sampleProjectOid}/documents/${sampleId}$`),
    invoke: () => projectsApi.deleteDocument(sampleProjectOid, sampleId)
  },
  {
    id: 'projects.bindSubcontractor',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/projects/${sampleProjectOid}/subcontractors$`),
    invoke: () => projectsApi.bindSubcontractor(sampleProjectOid, sampleId, 'construction')
  },
  {
    id: 'projects.updateSubcontractorBinding',
    kind: 'http',
    method: 'PUT',
    pathPattern: new RegExp(`^/api/projects/${sampleProjectOid}/subcontractors/${sampleId}$`),
    invoke: () => projectsApi.updateSubcontractorBinding(sampleProjectOid, sampleId, 'construction')
  },

  // matrix
  { id: 'matrix.list', kind: 'http', method: 'GET', pathPattern: /^\/api\/approval-matrix$/, invoke: () => matrixApi.list() },
  {
    id: 'matrix.get',
    kind: 'http',
    method: 'GET',
    pathPattern: new RegExp(`^/api/projects/${sampleProjectOid}/subcontractors/${sampleId}/matrix$`),
    invoke: () => matrixApi.get(sampleProjectOid, sampleId)
  },
  {
    id: 'matrix.set',
    kind: 'http',
    method: 'PUT',
    pathPattern: new RegExp(`^/api/projects/${sampleProjectOid}/subcontractors/${sampleId}/matrix$`),
    invoke: () => matrixApi.set(sampleProjectOid, sampleId, [])
  },

  // employees
  { id: 'employees.list', kind: 'http', method: 'GET', pathPattern: /^\/api\/employees$/, invoke: () => employeesApi.list() },
  { id: 'employees.create', kind: 'http', method: 'POST', pathPattern: /^\/api\/employees$/, invoke: () => employeesApi.create(sampleProjectOid, 'N', 'P', '+7', '123') },
  {
    id: 'employees.update',
    kind: 'http',
    method: 'PUT',
    pathPattern: new RegExp(`^/api/employees/${sampleId}$`),
    invoke: () => employeesApi.update(sampleId, 'N', 'P', '+7', '123')
  },
  {
    id: 'employees.remove',
    kind: 'http',
    method: 'DELETE',
    pathPattern: new RegExp(`^/api/employees/${sampleId}$`),
    invoke: () => employeesApi.remove(sampleId)
  },
  {
    id: 'employees.uploadPhoto',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/employees/${sampleId}/photo$`),
    invoke: () => employeesApi.uploadPhoto(sampleId, sampleFile)
  },
  {
    id: 'employees.fetchPhotoObjectUrl',
    kind: 'http',
    method: 'GET',
    pathPattern: new RegExp(`^/api/employees/${sampleId}/photo$`),
    invoke: () => employeesApi.fetchPhotoObjectUrl(sampleId, 'photos/x.jpg')
  },
  {
    id: 'employees.submit',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/employees/${sampleId}/submit$`),
    invoke: () => employeesApi.submit(sampleId)
  },
  {
    id: 'employees.resubmit',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/employees/${sampleId}/resubmit$`),
    invoke: () => employeesApi.resubmit(sampleId)
  },
  {
    id: 'employees.approvals',
    kind: 'http',
    method: 'GET',
    pathPattern: new RegExp(`^/api/employees/${sampleId}/approvals$`),
    invoke: () => employeesApi.approvals(sampleId)
  },
  {
    id: 'employees.accessPass',
    kind: 'http',
    method: 'GET',
    pathPattern: new RegExp(`^/api/employees/${sampleId}/access-pass$`),
    invoke: () => employeesApi.accessPass(sampleId)
  },
  {
    id: 'employees.accessPassQrUrl',
    kind: 'url',
    pathPattern: new RegExp(`^http://test/api/employees/${sampleId}/access-pass/qr\\.png$`),
    invoke: () => employeesApi.accessPassQrUrl(sampleId)
  },
  {
    id: 'employees.siteVisits',
    kind: 'http',
    method: 'GET',
    pathPattern: new RegExp(`^/api/employees/${sampleId}/site-visits$`),
    invoke: () => employeesApi.siteVisits(sampleId)
  },
  {
    id: 'employees.ppe',
    kind: 'http',
    method: 'GET',
    pathPattern: new RegExp(`^/api/employees/${sampleId}/ppe$`),
    invoke: () => employeesApi.ppe(sampleId)
  },
  {
    id: 'employees.issuePpe',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/employees/${sampleId}/ppe$`),
    invoke: () => employeesApi.issuePpe(sampleId, 'helmet')
  },
  {
    id: 'employees.returnPpe',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/employees/${sampleId}/ppe/${sampleProjectOid}/return$`),
    invoke: () => employeesApi.returnPpe(sampleId, sampleProjectOid)
  },
  {
    id: 'employees.documents',
    kind: 'http',
    method: 'GET',
    pathPattern: new RegExp(`^/api/employees/${sampleId}/documents$`),
    invoke: () => employeesApi.documents(sampleId)
  },
  {
    id: 'employees.uploadDocument',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/employees/${sampleId}/documents$`),
    invoke: () => employeesApi.uploadDocument(sampleId, sampleFile, 'doc', 'passport')
  },
  {
    id: 'employees.deleteDocument',
    kind: 'http',
    method: 'DELETE',
    pathPattern: new RegExp(`^/api/employees/${sampleId}/documents/${sampleProjectOid}$`),
    invoke: () => employeesApi.deleteDocument(sampleId, sampleProjectOid)
  },
  {
    id: 'employees.documentFileUrl',
    kind: 'url',
    pathPattern: new RegExp(`^http://test/api/employees/${sampleId}/documents/${sampleProjectOid}/file$`),
    invoke: () => employeesApi.documentFileUrl(sampleId, sampleProjectOid)
  },
  {
    id: 'employees.blocks',
    kind: 'http',
    method: 'GET',
    pathPattern: new RegExp(`^/api/employees/${sampleId}/blocks$`),
    invoke: () => employeesApi.blocks(sampleId)
  },
  {
    id: 'employees.block',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/employees/${sampleId}/block$`),
    invoke: () => employeesApi.block(sampleId, 'reason')
  },

  // photo reviews
  { id: 'photoReviews.pending', kind: 'http', method: 'GET', pathPattern: /^\/api\/employees\/photo-reviews\/pending$/, invoke: () => photoReviewsApi.pending() },
  {
    id: 'photoReviews.status',
    kind: 'http',
    method: 'GET',
    pathPattern: new RegExp(`^/api/employees/${sampleId}/photo-review$`),
    invoke: () => photoReviewsApi.status(sampleId)
  },
  {
    id: 'photoReviews.approve',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/employees/${sampleId}/photo-review/approve$`),
    invoke: () => photoReviewsApi.approve(sampleId)
  },
  {
    id: 'photoReviews.reject',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/employees/${sampleId}/photo-review/reject$`),
    invoke: () => photoReviewsApi.reject(sampleId, 'bad')
  },
  {
    id: 'photoReviews.photoUrl',
    kind: 'url',
    pathPattern: new RegExp(`^http://test/api/employees/${sampleId}/photo$`),
    invoke: () => photoReviewsApi.photoUrl(sampleId)
  },

  // approvals
  { id: 'approvals.inbox', kind: 'http', method: 'GET', pathPattern: /^\/api\/approvals\/inbox$/, invoke: () => approvalsApi.inbox() },
  {
    id: 'approvals.approve',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/approvals/${sampleId}/approve$`),
    invoke: () => approvalsApi.approve(sampleId)
  },
  {
    id: 'approvals.reject',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/approvals/${sampleId}/reject$`),
    invoke: () => approvalsApi.reject(sampleId, 'no')
  },

  // employee batches
  { id: 'employeeBatches.list', kind: 'http', method: 'GET', pathPattern: /^\/api\/employee-batches$/, invoke: () => employeeBatchesApi.list() },
  {
    id: 'employeeBatches.get',
    kind: 'http',
    method: 'GET',
    pathPattern: new RegExp(`^/api/employee-batches/${sampleId}$`),
    invoke: () => employeeBatchesApi.get(sampleId)
  },
  { id: 'employeeBatches.create', kind: 'http', method: 'POST', pathPattern: /^\/api\/employee-batches$/, invoke: () => employeeBatchesApi.create(sampleProjectOid, 'batch') },
  {
    id: 'employeeBatches.addMembers',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/employee-batches/${sampleId}/members$`),
    invoke: () => employeeBatchesApi.addMembers(sampleId, [sampleProjectOid])
  },
  {
    id: 'employeeBatches.removeMember',
    kind: 'http',
    method: 'DELETE',
    pathPattern: new RegExp(`^/api/employee-batches/${sampleId}/members/${sampleProjectOid}$`),
    invoke: () => employeeBatchesApi.removeMember(sampleId, sampleProjectOid)
  },
  {
    id: 'employeeBatches.submit',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/employee-batches/${sampleId}/submit$`),
    invoke: () => employeeBatchesApi.submit(sampleId)
  },
  {
    id: 'employeeBatches.remove',
    kind: 'http',
    method: 'DELETE',
    pathPattern: new RegExp(`^/api/employee-batches/${sampleId}$`),
    invoke: () => employeeBatchesApi.remove(sampleId)
  },

  // document requests
  { id: 'documentRequests.list', kind: 'http', method: 'GET', pathPattern: /^\/api\/document-requests$/, invoke: () => documentRequestsApi.list() },
  {
    id: 'documentRequests.create',
    kind: 'http',
    method: 'POST',
    pathPattern: /^\/api\/document-requests$/,
    invoke: () => documentRequestsApi.create(sampleProjectOid, 'leave', 't', 'd')
  },
  {
    id: 'documentRequests.update',
    kind: 'http',
    method: 'PUT',
    pathPattern: new RegExp(`^/api/document-requests/${sampleId}$`),
    invoke: () => documentRequestsApi.update(sampleId, 't', 'd')
  },
  {
    id: 'documentRequests.remove',
    kind: 'http',
    method: 'DELETE',
    pathPattern: new RegExp(`^/api/document-requests/${sampleId}$`),
    invoke: () => documentRequestsApi.remove(sampleId)
  },
  {
    id: 'documentRequests.submit',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/document-requests/${sampleId}/submit$`),
    invoke: () => documentRequestsApi.submit(sampleId)
  },
  {
    id: 'documentRequests.resubmit',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/document-requests/${sampleId}/resubmit$`),
    invoke: () => documentRequestsApi.resubmit(sampleId)
  },
  {
    id: 'documentRequests.approvals',
    kind: 'http',
    method: 'GET',
    pathPattern: new RegExp(`^/api/document-requests/${sampleId}/approvals$`),
    invoke: () => documentRequestsApi.approvals(sampleId)
  },
  { id: 'documentRequests.inbox', kind: 'http', method: 'GET', pathPattern: /^\/api\/document-request-approvals\/inbox$/, invoke: () => documentRequestsApi.inbox() },
  {
    id: 'documentRequests.approve',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/document-request-approvals/${sampleId}/approve$`),
    invoke: () => documentRequestsApi.approve(sampleId)
  },
  {
    id: 'documentRequests.reject',
    kind: 'http',
    method: 'POST',
    pathPattern: new RegExp(`^/api/document-request-approvals/${sampleId}/reject$`),
    invoke: () => documentRequestsApi.reject(sampleId, 'no')
  },

  // document matrix
  { id: 'documentMatrix.list', kind: 'http', method: 'GET', pathPattern: /^\/api\/document-matrix\/summaries$/, invoke: () => documentMatrixApi.list() },
  { id: 'documentMatrix.get', kind: 'http', method: 'GET', pathPattern: /^\/api\/document-matrix$/, invoke: () => documentMatrixApi.get(sampleProjectOid, sampleId, 'leave') },
  { id: 'documentMatrix.set', kind: 'http', method: 'PUT', pathPattern: /^\/api\/document-matrix$/, invoke: () => documentMatrixApi.set(sampleProjectOid, sampleId, 'leave', []) },

  // site visit journal
  { id: 'siteVisitJournal.list', kind: 'http', method: 'GET', pathPattern: /^\/api\/site-visit-journal$/, invoke: () => siteVisitJournalApi.list() },
  {
    id: 'siteVisitJournal.exportFile',
    kind: 'http',
    method: 'GET',
    pathPattern: /^\/api\/site-visit-journal\/export$/,
    invoke: () => siteVisitJournalApi.exportFile('pdf')
  },

  // audit
  { id: 'audit.list', kind: 'http', method: 'GET', pathPattern: /^\/api\/audit-events$/, invoke: () => auditApi.list({ page: 1 }) },

  // reports
  { id: 'reports.compliance', kind: 'http', method: 'GET', pathPattern: /^\/api\/reports\/subcontractor-compliance$/, invoke: () => reportsApi.compliance() },
  {
    id: 'reports.exportApprovedPersonnel',
    kind: 'http',
    method: 'GET',
    pathPattern: /^\/api\/reports\/approved-personnel\/export$/,
    invoke: () => reportsApi.exportApprovedPersonnel('csv')
  },
  {
    id: 'reports.exportSiteVisits',
    kind: 'http',
    method: 'GET',
    pathPattern: /^\/api\/reports\/site-visits\/export$/,
    invoke: () => reportsApi.exportSiteVisits('csv')
  },
  {
    id: 'reports.exportBlocks',
    kind: 'http',
    method: 'GET',
    pathPattern: /^\/api\/reports\/employee-blocks\/export$/,
    invoke: () => reportsApi.exportBlocks('csv')
  },
  {
    id: 'reports.exportDocumentRequests',
    kind: 'http',
    method: 'GET',
    pathPattern: /^\/api\/reports\/document-requests\/export$/,
    invoke: () => reportsApi.exportDocumentRequests('csv')
  },
  {
    id: 'reports.exportExpiringDocuments',
    kind: 'http',
    method: 'GET',
    pathPattern: /^\/api\/reports\/expiring-documents\/export$/,
    invoke: () => reportsApi.exportExpiringDocuments('csv', 14)
  },

  // delegations
  { id: 'delegations.list', kind: 'http', method: 'GET', pathPattern: /^\/api\/delegations$/, invoke: () => delegationsApi.list(true) },
  {
    id: 'delegations.create',
    kind: 'http',
    method: 'POST',
    pathPattern: /^\/api\/delegations$/,
    invoke: () =>
      delegationsApi.create({
        delegateUserId: sampleId,
        validFrom: new Date().toISOString(),
        validTo: new Date(Date.now() + 86400000).toISOString()
      })
  },
  {
    id: 'delegations.revoke',
    kind: 'http',
    method: 'DELETE',
    pathPattern: new RegExp(`^/api/delegations/${sampleId}$`),
    invoke: () => delegationsApi.revoke(sampleId)
  },

  // incidents
  { id: 'incidents.list', kind: 'http', method: 'GET', pathPattern: /^\/api\/incidents$/, invoke: () => incidentsApi.list() },
  {
    id: 'incidents.create',
    kind: 'http',
    method: 'POST',
    pathPattern: /^\/api\/incidents$/,
    invoke: () =>
      incidentsApi.create({
        projectOid: sampleProjectOid,
        occurredAt: new Date().toISOString(),
        title: 't',
        description: 'd',
        severity: 'low',
        blockUntilResolved: false,
        employeeIds: []
      })
  },
  {
    id: 'incidents.updateStatus',
    kind: 'http',
    method: 'PATCH',
    pathPattern: new RegExp(`^/api/incidents/${sampleId}$`),
    invoke: () => incidentsApi.updateStatus(sampleId, 'resolved')
  },

  // zup
  { id: 'zup.employees', kind: 'http', method: 'GET', pathPattern: /^\/api\/zup\/employees$/, invoke: () => zupApi.employees('tansu_construction') },
  { id: 'zup.projects', kind: 'http', method: 'GET', pathPattern: /^\/api\/zup\/projects$/, invoke: () => zupApi.projects() }
];
